using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace OAuth2Bridge;

public sealed class OAuthServer : IDisposable
{
    private readonly string _clientId;
    private readonly string? _clientSecret;
    private string _redirectUri;
    private int _port;
    private readonly OAuthLogger _logger;
    private readonly DiscordApiClient _api;
    private bool _disposed;

    public List<DiscordScopes> Scopes { get; } = new();
    public bool EnablePkce { get; set; }
    public string Prompt { get; set; } = "consent";
    public Dictionary<string,string> AdditionalAuthParams { get; } = new();
    public string? SuccessHtmlPath { get; set; } = "./data/success.html";
    public string? ErrorHtmlPath { get; set; }
    public TokenResponse? LastTokenResponse { get; private set; }
    public event Action<TokenResponse>? OnTokenRefreshed;

    public int Port => _port;
    public string RedirectUri => _redirectUri;

    public OAuthServer(string clientId, string? clientSecret, int port, OAuthLogger logger, string appName = "OAuth2Bridge")
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("clientId required", nameof(clientId));
        if (string.IsNullOrWhiteSpace(clientSecret))
            new OAuthLogger(NullLogger<OAuthLogger>.Instance).LogWarning("clientSecret empty - use EnablePkce=true for public clients");
        _clientId = clientId;
        _clientSecret = string.IsNullOrWhiteSpace(clientSecret) ? null : clientSecret;
        _port = port == 0 ? GetFreePort() : port;
        _redirectUri = $"http://localhost:{_port}/callback";
        _logger = logger ?? new OAuthLogger(NullLogger<OAuthLogger>.Instance);
        _api = new DiscordApiClient(_logger);
        AppName = appName ?? "OAuth2Bridge";
    }

    public string AppName { get; }

    public OAuthServer(OAuthServerOptions options, OAuthLogger? logger = null)
        : this(options.ClientId, options.ClientSecret, options.Port, logger ?? new OAuthLogger(NullLogger<OAuthLogger>.Instance), options.AppName)
    {
        if (options.Scopes != null) foreach (var s in options.Scopes) Scopes.Add(s);
        EnablePkce = options.EnablePkce;
        Prompt = options.Prompt ?? "consent";
        if (options.AdditionalAuthParams != null) foreach (var kv in options.AdditionalAuthParams) AdditionalAuthParams[kv.Key] = kv.Value;
        SuccessHtmlPath = options.SuccessHtmlPath;
        ErrorHtmlPath = options.ErrorHtmlPath;
    }

    public static OAuthServer CreateServer(string clientId, string? clientSecret, int port = 5000, OAuthLogger? logger = null, string appName = "OAuth2Bridge")
        => new(clientId, clientSecret, port, logger ?? new OAuthLogger(NullLogger<OAuthLogger>.Instance), appName);

    public static OAuthServer CreateServer(OAuthServerOptions options, OAuthLogger? logger = null) => new(options, logger);

    [Obsolete("Use CreateServer(OAuthServerOptions) to avoid hardcoding secrets")]
    public static OAuthServer CreateServerWithSecret(string clientId, string clientSecret, int port = 5000, OAuthLogger? logger = null, string appName = "OAuth2Bridge")
        => CreateServer(clientId, clientSecret, port, logger, appName);

    public void AddScope(DiscordScopes scope) => Scopes.Add(scope);

    public Task<UserInfo> AuthenticateAsync(CancellationToken ct, string htmlCallbackPath = "./data/success.html", TimeSpan? timeout = null)
        => AuthenticateWithTokenAsync(ct, htmlCallbackPath, timeout).ContinueWith(t => t.Result.User, ct);

    public Task<UserInfo> AuthenticateAsync(CancellationToken ct, string htmlCallbackPath, string? errorHtmlPath, TimeSpan? timeout = null)
        => AuthenticateWithTokenAsync(ct, htmlCallbackPath, errorHtmlPath, timeout).ContinueWith(t => t.Result.User, ct);

    public Task<(UserInfo User, TokenResponse Token)> AuthenticateWithTokenAsync(CancellationToken ct, string htmlCallbackPath = "./data/success.html", TimeSpan? timeout = null)
        => AuthenticateWithTokenAsync(ct, htmlCallbackPath, null, timeout);

    public Task<(UserInfo User, TokenResponse Token)> AuthenticateWithTokenAsync(CancellationToken ct, string? htmlCallbackPath, string? errorHtmlPath, TimeSpan? timeout)
        => AuthenticateInternalAsync(ct, htmlCallbackPath ?? SuccessHtmlPath, errorHtmlPath ?? ErrorHtmlPath, timeout);

    private async Task<(UserInfo User, TokenResponse Token)> AuthenticateInternalAsync(CancellationToken ct, string? htmlCallbackPath, string? errorHtmlPath, TimeSpan? timeout)
    {
        if (Scopes.Count == 0) _logger.LogInformation("No scopes configured");
        var state = GenerateState();
        string? verifier = null, challenge = null;
        if (EnablePkce) (verifier, challenge) = GeneratePkcePair();

        var url = AuthorizationUrlBuilder.Build(_clientId, _redirectUri, Scopes, state, Prompt, EnablePkce, challenge, AdditionalAuthParams);
        _logger.LogInformation($"Opening {url}");
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { _logger.LogError($"Open URL failed: {ex.Message}"); Console.WriteLine($"Open manually: {url}"); }

        using var server = new LocalCallbackServer(_redirectUri, _logger);
        server.Start();
        _logger.LogInformation($"Listening { _redirectUri }/");

        using var timeoutCts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts?.Token ?? CancellationToken.None);
        var linked = linkedCts.Token;
        using var reg = linked.Register(() => { try { server.Stop(); } catch {} });

        try
        {
            var ctx = await server.WaitForCallbackAsync(linked).ConfigureAwait(false);
            var req = ctx.Request; var resp = ctx.Response;

            var err = req.QueryString["error"];
            if (!string.IsNullOrEmpty(err))
            {
                var desc = req.QueryString["error_description"] ?? err;
                var htmlErr = Helper.GenerateErrorHtml(_logger, errorHtmlPath, $"Auth failed: {desc}", AppName);
                await LocalCallbackServer.WriteHtmlAsync(resp, htmlErr, 400, linked).ConfigureAwait(false);
                throw new OAuthException($"Auth failed: {desc}");
            }
            if (req.QueryString["state"] != state)
            {
                var htmlErr = Helper.GenerateErrorHtml(_logger, errorHtmlPath, "Invalid state", AppName);
                await LocalCallbackServer.WriteHtmlAsync(resp, htmlErr, 400, linked).ConfigureAwait(false);
                throw new OAuthException("Invalid state - CSRF");
            }
            var code = req.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                var htmlErr = Helper.GenerateErrorHtml(_logger, errorHtmlPath, "No code", AppName);
                await LocalCallbackServer.WriteHtmlAsync(resp, htmlErr, 400, linked).ConfigureAwait(false);
                throw new OAuthException("No code received");
            }

            var token = await _api.ExchangeCodeAsync(_clientId, _clientSecret, _redirectUri, code, verifier, linked).ConfigureAwait(false);
            LastTokenResponse = token;
            var user = await _api.GetUserAsync(token.AccessToken, linked).ConfigureAwait(false);

            var html = Helper.GenerateHtmlFromFile(_logger, htmlCallbackPath, user.Username, Helper.GetUserAvatar(user), user.Email ?? string.Empty, AppName);
            await LocalCallbackServer.WriteSuccessAsync(resp, html, linked).ConfigureAwait(false);
            return (user, token);
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true) { _logger.LogError("Timeout"); throw new OAuthException("Timed out"); }
        catch (OperationCanceledException) { _logger.LogError("Cancelled"); throw; }
        catch (OAuthException) { throw; }
        catch (HttpListenerException ex) when (linked.IsCancellationRequested) { throw new OperationCanceledException("Listener stopped", ex, linked); }
        catch (Exception ex) { _logger.LogError($"Auth failed: {ex.Message}"); throw new OAuthException($"Auth failed: {ex.Message}"); }
    }

    public Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => _api.RefreshAsync(_clientId, _clientSecret, refreshToken, ct).ContinueWith(t => { LastTokenResponse = t.Result; OnTokenRefreshed?.Invoke(t.Result); return t.Result; }, ct);

    public Task<TokenResponse> GetValidAccessTokenAsync(CancellationToken ct = default)
    {
        if (LastTokenResponse == null) throw new OAuthException("No token cached");
        if (!LastTokenResponse.IsExpired) return Task.FromResult(LastTokenResponse);
        if (string.IsNullOrEmpty(LastTokenResponse.RefreshToken)) throw new OAuthException("Expired and no refresh_token");
        return RefreshTokenAsync(LastTokenResponse.RefreshToken, ct);
    }

    public Task RevokeTokenAsync(string token, string? hint = null, CancellationToken ct = default) => _api.RevokeAsync(_clientId, _clientSecret, token, hint, ct);
    public Task<UserInfo> GetUserInfoAsync(string token, CancellationToken ct = default) => _api.GetUserAsync(token, ct);
    public Task<List<GuildInfo>> GetUserGuildsAsync(string token, CancellationToken ct = default) => _api.GetGuildsAsync(token, ct);
    public Task<GuildMemberInfo> GetGuildMemberAsync(string token, string guildId, CancellationToken ct = default) => _api.GetGuildMemberAsync(token, guildId, ct);
    public Task<List<ConnectionInfo>> GetConnectionsAsync(string token, CancellationToken ct = default) => _api.GetConnectionsAsync(token, ct);
    public Task<AuthorizationInfo> GetAuthorizationInfoAsync(string token, CancellationToken ct = default) => _api.GetAuthInfoAsync(token, ct);
    public Task<RoleConnectionInfo> GetRoleConnectionAsync(string token, CancellationToken ct = default) => _api.GetRoleConnectionAsync(_clientId, token, ct);
    public Task<RoleConnectionInfo> UpdateRoleConnectionAsync(string token, RoleConnectionInfo conn, CancellationToken ct = default) => _api.UpdateRoleConnectionAsync(_clientId, token, conn, ct);
    public Task<List<EntitlementInfo>> GetEntitlementsAsync(string token, CancellationToken ct = default) => _api.GetEntitlementsAsync(token, ct);
    public static string BuildBotInviteUrl(string clientId, long permissions = 0, string? guildId = null, bool disableGuildSelect = false, string? redirectUri = null, string[]? scopes = null)
        => AuthorizationUrlBuilder.BuildBotInvite(clientId, permissions, guildId, disableGuildSelect, redirectUri, scopes);

    private static string GenerateState()
    {
#if NET8_0_OR_GREATER
        Span<byte> b = stackalloc byte[32]; RandomNumberGenerator.Fill(b); return Convert.ToHexString(b).ToLowerInvariant();
#else
        byte[] b = new byte[32]; using(var r=RandomNumberGenerator.Create()) r.GetBytes(b); var sb=new StringBuilder(64); foreach(var x in b) sb.Append(x.ToString("x2")); return sb.ToString();
#endif
    }

    private static (string verifier, string challenge) GeneratePkcePair()
    {
        byte[] b = new byte[32];
#if NET8_0_OR_GREATER
        RandomNumberGenerator.Fill(b);
#else
        using(var r=RandomNumberGenerator.Create()) r.GetBytes(b);
#endif
        string v = Base64Url(b);
        using var sha = SHA256.Create();
        string c = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(v)));
        return (v,c);
    }

    private static string Base64Url(byte[] d) => Convert.ToBase64String(d).Replace('+','-').Replace('/','_').TrimEnd('=');
    private static int GetFreePort() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); int p = ((IPEndPoint)l.LocalEndpoint).Port; l.Stop(); return p; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}