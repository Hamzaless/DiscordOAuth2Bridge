using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace OAuth2Bridge;

internal sealed class DiscordApiClient
{
    private readonly OAuthLogger _logger;
    private static readonly Random _random = new Random();

    public DiscordApiClient(OAuthLogger logger) { _logger = logger; }

#if NET8_0_OR_GREATER
    private static readonly SocketsHttpHandler Handler = new() { PooledConnectionLifetime = TimeSpan.FromMinutes(2), PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1) };
    private static readonly HttpClient Client = new(Handler, false) { Timeout = TimeSpan.FromSeconds(30) };
#else
    private static readonly HttpClient Client = new(new HttpClientHandler(), false) { Timeout = TimeSpan.FromSeconds(30) };
#endif

    public async Task<TokenResponse> ExchangeCodeAsync(string clientId, string? clientSecret, string redirectUri, string code, string? verifier, CancellationToken ct)
    {
        var list = new List<KeyValuePair<string,string>> { new("client_id", clientId), new("grant_type", "authorization_code"), new("code", code), new("redirect_uri", redirectUri) };
        if (!string.IsNullOrEmpty(clientSecret)) list.Add(new("client_secret", clientSecret));
        if (!string.IsNullOrEmpty(verifier)) list.Add(new("code_verifier", verifier));
        var content = new FormUrlEncodedContent(list);
        using var resp = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token") { Content = content }, ct).ConfigureAwait(false);
        var str = await ReadStringAsync(resp, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new OAuthException("Failed to get access token: " + str);
        _logger.LogInformation("Access token received");
        var token = JsonSerializer.Deserialize(str, DiscordJsonContext.Default.TokenResponse);
        if (token != null && !string.IsNullOrEmpty(token.AccessToken)) { token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn); return token; }
        try { var jo = Newtonsoft.Json.Linq.JObject.Parse(str); var t = jo["access_token"]?.ToString(); if (!string.IsNullOrEmpty(t)) { var tr = new TokenResponse{ AccessToken=t, Scope=jo["scope"]?.ToString()??"", TokenType=jo["token_type"]?.ToString()??"Bearer", ExpiresIn=jo["expires_in"]?.ToObject<int>()??0, RefreshToken=jo["refresh_token"]?.ToString()}; tr.ExpiresAt = DateTime.UtcNow.AddSeconds(tr.ExpiresIn); try{ tr.Webhook = jo["webhook"]?.ToObject<WebhookInfo>(); tr.Guild = jo["guild"]?.ToObject<GuildInfo>(); } catch{} return tr; } } catch {}
        throw new OAuthException("Failed to parse token response");
    }

    public async Task<TokenResponse> RefreshAsync(string clientId, string? clientSecret, string refreshToken, CancellationToken ct)
    {
        var list = new List<KeyValuePair<string,string>> { new("client_id", clientId), new("grant_type", "refresh_token"), new("refresh_token", refreshToken) };
        if (!string.IsNullOrEmpty(clientSecret)) list.Add(new("client_secret", clientSecret));
        var content = new FormUrlEncodedContent(list);
        using var resp = await SendWithRetryAsync(() => new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token") { Content = content }, ct).ConfigureAwait(false);
        var str = await ReadStringAsync(resp, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new OAuthException("Failed to refresh token: " + str);
        var token = JsonSerializer.Deserialize(str, DiscordJsonContext.Default.TokenResponse) ?? throw new OAuthException("Failed to parse refresh");
        token.ExpiresAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn);
        return token;
    }

    public async Task RevokeAsync(string clientId, string? clientSecret, string token, string? hint, CancellationToken ct)
    {
        var dict = new List<KeyValuePair<string,string>> { new("token", token) };
        if (!string.IsNullOrEmpty(hint)) dict.Add(new("token_type_hint", hint));
        HttpRequestMessage Factory()
        {
            var c = new FormUrlEncodedContent(dict);
            var req = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token/revoke") { Content = c };
            if (!string.IsNullOrEmpty(clientSecret))
            {
                var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
            }
            else
            {
                dict.Add(new("client_id", clientId));
                req.Content = new FormUrlEncodedContent(dict);
            }
            return req;
        }
        using var resp = await SendWithRetryAsync(Factory, ct).ConfigureAwait(false);
        var str = await ReadStringAsync(resp, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new OAuthException("Failed to revoke token: " + str);
    }

    public Task<UserInfo> GetUserAsync(string token, CancellationToken ct) => GetAsync<UserInfo>("https://discord.com/api/users/@me", token, ct, DiscordJsonContext.Default.UserInfo);
    public Task<List<GuildInfo>> GetGuildsAsync(string token, CancellationToken ct) => GetAsync<List<GuildInfo>>("https://discord.com/api/users/@me/guilds", token, ct, DiscordJsonContext.Default.ListGuildInfo);
    public Task<GuildMemberInfo> GetGuildMemberAsync(string token, string guildId, CancellationToken ct) => GetAsync<GuildMemberInfo>($"https://discord.com/api/users/@me/guilds/{guildId}/member", token, ct, DiscordJsonContext.Default.GuildMemberInfo);
    public Task<List<ConnectionInfo>> GetConnectionsAsync(string token, CancellationToken ct) => GetAsync<List<ConnectionInfo>>("https://discord.com/api/users/@me/connections", token, ct, DiscordJsonContext.Default.ListConnectionInfo);
    public Task<AuthorizationInfo> GetAuthInfoAsync(string token, CancellationToken ct) => GetAsync<AuthorizationInfo>("https://discord.com/api/oauth2/@me", token, ct, DiscordJsonContext.Default.AuthorizationInfo);
    public Task<RoleConnectionInfo> GetRoleConnectionAsync(string clientId, string token, CancellationToken ct) => GetAsync<RoleConnectionInfo>($"https://discord.com/api/users/@me/applications/{clientId}/role-connection", token, ct, DiscordJsonContext.Default.RoleConnectionInfo);
    public Task<List<EntitlementInfo>> GetEntitlementsAsync(string token, CancellationToken ct) => GetAsync<List<EntitlementInfo>>("https://discord.com/api/users/@me/entitlements", token, ct, DiscordJsonContext.Default.ListEntitlementInfo);

    public async Task<RoleConnectionInfo> UpdateRoleConnectionAsync(string clientId, string token, RoleConnectionInfo conn, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(conn, DiscordJsonContext.Default.RoleConnectionInfo);
        using var resp = await SendWithRetryAsync(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Put, $"https://discord.com/api/users/@me/applications/{clientId}/role-connection") { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return req;
        }, ct).ConfigureAwait(false);
        var str = await ReadStringAsync(resp, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new OAuthException("Failed to update role connection: " + str);
        return JsonSerializer.Deserialize(str, DiscordJsonContext.Default.RoleConnectionInfo) ?? conn;
    }

    private async Task<T> GetAsync<T>(string url, string token, CancellationToken ct, JsonTypeInfo<T> typeInfo)
    where T : class
    {
        using var resp = await SendWithRetryAsync(() =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return req;
        }, ct).ConfigureAwait(false);
        var str = await ReadStringAsync(resp, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) throw new OAuthException($"GET {url} failed: {str}");
        return JsonSerializer.Deserialize(str, typeInfo) ?? throw new OAuthException("Failed to deserialize");
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> factory, CancellationToken ct, int maxRetries = 2)
    {
        for (int i = 0; i <= maxRetries; i++)
        {
            using var req = factory();
            var resp = await Client.SendAsync(req, ct).ConfigureAwait(false);
            if ((int)resp.StatusCode != 429) return resp;
            double rnd;
            lock (_random) rnd = _random.NextDouble();
            var after = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1 + rnd);
            double jitterMs;
            lock (_random) jitterMs = _random.Next(0, 500);
            var jitter = TimeSpan.FromMilliseconds(jitterMs);
            var delay = after + jitter;
            _logger.LogWarning($"429 retry {i+1}/{maxRetries} after {delay.TotalSeconds:F1}s");
            resp.Dispose();
            if (i == maxRetries) throw new OAuthException("Rate limited");
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }
        throw new OAuthException("Retry exhausted");
    }

    private static Task<string> ReadStringAsync(HttpResponseMessage r, CancellationToken ct)
    {
#if NET8_0_OR_GREATER
        return r.Content.ReadAsStringAsync(ct);
#else
        return r.Content.ReadAsStringAsync();
#endif
    }
}