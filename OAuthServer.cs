using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace OAuth2Bridge
{
    

    

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

        // Scopes are the permissions requested from Discord OAuth2
        public List<DiscordScopes> Scopes { get; set; } = new();

        public OAuthServer(string clientId, string clientSecret, int port, OAuthLogger logger, string appName = "OAuth2Bridge")
        {
            _appName = appName;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _port = port;
            _redirectUri = $"http://localhost:{port}/callback"; // Localhost callback URI
            _logger = logger;
        }

        // Static factory method for easy server creation
        public static OAuthServer CreateServer(string clientId, string clientSecret, int port = 5000, OAuthLogger logger = null, string appName = "OAuth2Bridge")
        {
            return new OAuthServer(clientId, clientSecret, port, logger ?? new OAuthLogger(new LoggerFactory().CreateLogger<OAuthLogger>()), appName);
        }

        // Helper method to generate the HTML content from file or template
        

        // The AuthenticateAsync method handles OAuth authentication
        public async Task<UserInfo> AuthenticateAsync(CancellationToken cancellationToken, int timeoutSeconds = 30, string htmlCallbackPath = "./data/success.html")
        {
            // Construct the authentication URL
            string scopeParam = Uri.EscapeDataString(string.Join(" ", Scopes.Select(scope => scope.ToString().ToLower().Replace("ı", "i"))));
            string authUrl = $"https://discord.com/api/oauth2/authorize?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope={scopeParam}";

            _logger.LogInformation($"Opening URL: {authUrl}");
            OpenUrl(authUrl);

            _listener = new HttpListener();
            _listener.Prefixes.Add(_redirectUri + "/");
            _listener.Start();

            _logger.LogInformation("Listening for authentication callback...");

            var cts = new CancellationTokenSource(timeoutSeconds * 1000);
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token;

            HttpListenerContext context = null;

            try
            {
                // Wait for either a timeout or a successful authentication callback
                var completedTask = await Task.WhenAny(_listener.GetContextAsync(), Task.Delay(timeoutSeconds * 1000, combinedToken));

                if (completedTask == Task.Delay(timeoutSeconds * 1000, combinedToken)) // Timeout occurred
                {
                    throw new OAuthException("Authentication timeout exceeded.");
                }

                // The authentication task completed successfully
                context = await _listener.GetContextAsync();

                var request = context.Request;
                var response = context.Response;

                string code = request.QueryString["code"];
                if (string.IsNullOrEmpty(code))
                {
                    throw new OAuthException("Authorization failed. No code received.");
                }

                // Exchange the authorization code for an access token
                string accessToken = await GetAccessTokenAsync(code);
                var userInfo = await GetUserInfoAsync(accessToken);

                // Generate success page HTML
                string htmlContent = Helper.GenerateHtmlFromFile(_logger , htmlCallbackPath, "@" + userInfo.Username, Helper.GetUserAvatar(userInfo), userInfo.Email, _appName);
                byte[] buffer = Encoding.UTF8.GetBytes(htmlContent);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, combinedToken);
                response.OutputStream.Close();

                _listener.Stop();
                _logger.LogInformation("Authentication completed successfully.");

                return userInfo;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Authentication timeout exceeded or operation canceled.");
                throw new OAuthException("Authentication failed: Timeout or canceled.");
            }
            finally
            {
                _listener?.Stop(); // Ensure the listener is stopped after completion
            }
        }

        // Helper method to get access token from Discord
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
                _logger.LogError($"Failed to get access token: {responseString}");
                throw new OAuthException("Failed to get access token: " + responseString);
            }

            _logger.LogInformation("Access token received successfully.");
            var json = JsonConvert.DeserializeObject<dynamic>(responseString);
            return json.access_token;
        }

        // Helper method to get user information using access token
        private async Task<UserInfo> GetUserInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            var response = await client.GetStringAsync("https://discord.com/api/users/@me");
            _logger.LogInformation("User info received successfully.");
            return JsonConvert.DeserializeObject<UserInfo>(response);
        }



        // Open the provided URL in the default browser
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
                _logger.LogError($"Failed to open URL: {ex.Message}");
                Console.WriteLine("Please open the following URL manually: " + url);
            }
        }
    }

}
