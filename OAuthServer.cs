using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace OAuth2Bridge
{
    /// <summary>
    /// Enum representing the various Discord OAuth2 scopes.
    /// </summary>
    public enum DiscordScopes
    {
        Identify,
        Email,
        Connections,
        Guilds,
        GuildsJoin,
        GuildsMembersRead,
        MessagesRead,
        RelationshipsRead,
        ActivitiesRead,
        ActivitiesWrite,
        ApplicationsBuildsRead,
        ApplicationsBuildsUpload,
        ApplicationsCommands,
        ApplicationsCommandsUpdate,
        ApplicationsEntitlements,
        ApplicationsStoreUpdate,
        Bot,
        WebhookIncoming,
        Rpc,
        RpcNotificationsRead,
        RpcVoiceWrite,
        RpcVoiceRead,
        RpcVideoWrite,
        RpcVideoRead,
        RpcScreenshareRead,
        RpcScreenshareWrite,
        RoleConnectionsWrite,
        Voice,
        PresencesRead,
        PresencesWrite,
        DMChannelsRead,
        DMChannelsMessagesWrite,
        PaymentSourcesCountryCode,
        OpenID,
        GatewayConnect,
        SDKSocialLayer,
        AccountGlobalNameUpdate
    }

    /// <summary>
    /// Logger class for OAuth operations.
    /// </summary>
    public class OAuthLogger
    {
        public event Action<string> OnLog;

        public void Log(string message)
        {
            OnLog?.Invoke(message);
        }
    }

    /// <summary>
    /// OAuth server class to handle Discord OAuth2 authentication.
    /// </summary>
    public class OAuthServer
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly int _port;
        private readonly OAuthLogger _logger;
        private HttpListener _listener;
        private string _appName;

        public List<DiscordScopes> Scopes { get; set; } = new();

        public OAuthServer(string clientId, string clientSecret, int port, OAuthLogger logger, string appName = "OAuth2Bridge")
        {
            _appName = appName;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _port = port;
            _redirectUri = $"http://localhost:{port}/callback";
            _logger = logger;
        }

        public static OAuthServer CreateServer(string clientId, string clientSecret, int port = 5000, OAuthLogger logger = null, string appName = "OAuth2Bridge")
        {
            return new OAuthServer(clientId, clientSecret, port, logger ?? new OAuthLogger(), appName);
        }

        public async Task<UserInfo> AuthenticateAsync()
        {
            string scopeParam = Uri.EscapeDataString(string.Join(" ", Scopes.Select(scope => scope.ToString().ToLower().Replace("ı", "i"))));
            string authUrl = $"https://discord.com/api/oauth2/authorize?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope={scopeParam}";

            _logger.Log($"Opening URL: {authUrl}");
            OpenUrl(authUrl);

            _listener = new HttpListener();
            _listener.Prefixes.Add(_redirectUri + "/");
            _listener.Start();

            _logger.Log("Listening for authentication callback...");

            var context = await _listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;
            string code = request.QueryString["code"];

            if (string.IsNullOrEmpty(code))
            {
                throw new Exception("Authorization failed. No code received.");
            }

            string accessToken = await GetAccessTokenAsync(code);
            var userInfo = await GetUserInfoAsync(accessToken);

            string htmlFromFile = File.ReadAllText("./data/success");
            htmlFromFile = htmlFromFile
                .Replace("%app_name%", _appName)
                .Replace("%profile_name%", "@" + userInfo.Username)
                .Replace("%profile_url%", GetUserAvatar(userInfo))
                .Replace("%profile_email%", userInfo.Email);
            byte[] buffer = Encoding.UTF8.GetBytes(htmlFromFile);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            _listener.Stop();
            _logger.Log("Authentication completed successfully.");

            return userInfo;
        }

        private async Task<string> GetAccessTokenAsync(string code)
        {
            using var client = new HttpClient();

            var values = new FormUrlEncodedContent(new[]
            {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", _redirectUri)
                });

            var response = await client.PostAsync("https://discord.com/api/oauth2/token", values);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.Log($"Failed to get access token: {responseString}");
                throw new Exception("Failed to get access token: " + responseString);
            }

            _logger.Log("Access token received successfully.");
            var json = JsonConvert.DeserializeObject<dynamic>(responseString);
            return json.access_token;
        }

        private async Task<UserInfo> GetUserInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            var response = await client.GetStringAsync("https://discord.com/api/users/@me");
            _logger.Log("User info received successfully.");
            return JsonConvert.DeserializeObject<UserInfo>(response);
        }

        public async Task<List<GuildInfo>> GetGuildsAsync(string accessToken)
        {
            if (!Scopes.Contains(DiscordScopes.Guilds))
            {
                throw new Exception("The 'guilds' scope is required to fetch guild information.");
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            var response = await client.GetStringAsync("https://discord.com/api/users/@me/guilds");
            _logger.Log("Guilds information received successfully.");
            return JsonConvert.DeserializeObject<List<GuildInfo>>(response);
        }

        public async Task<List<ConnectionInfo>> GetConnectionsAsync(string accessToken)
        {
            if (!Scopes.Contains(DiscordScopes.Connections))
            {
                throw new Exception("The 'connections' scope is required to fetch connection information.");
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            var response = await client.GetStringAsync("https://discord.com/api/users/@me/connections");
            _logger.Log("Connections information received successfully.");
            return JsonConvert.DeserializeObject<List<ConnectionInfo>>(response);
        }

        public string GetUserAvatar(UserInfo userInfo)
        {
            return $"https://cdn.discordapp.com/avatars/{userInfo.Id}/{userInfo.Avatar}";
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to open URL: {ex.Message}");
                Console.WriteLine("Please open the following URL manually: " + url);
            }
        }
    }

    /// <summary>
    /// Class representing user information.
    /// </summary>
    public class UserInfo
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Avatar { get; set; }
        public string Email { get; set; }
    }

    /// <summary>
    /// Class representing guild information.
    /// </summary>
    public class GuildInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
    }

    /// <summary>
    /// Class representing connection information.
    /// </summary>
    public class ConnectionInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
