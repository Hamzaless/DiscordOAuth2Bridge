using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace OAuth2Bridge
{
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
            return new OAuthServer(clientId, clientSecret, port, logger ?? new OAuthLogger(new LoggerFactory().CreateLogger<OAuthLogger>()), appName);
        }

        public async Task<UserInfo> AuthenticateAsync(CancellationToken cancellationToken, string htmlCallbackPath = "./data/success.html")
        {
            string scopeParam = Uri.EscapeDataString(string.Join(" ", Scopes.Select(scope => scope.ToString().ToLower().Replace("ı", "i"))));
            string authUrl = $"https://discord.com/api/oauth2/authorize?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope={scopeParam}";

            _logger.LogInformation($"Opening URL: {authUrl}");
            OpenUrl(authUrl);

            _listener = new HttpListener();
            _listener.Prefixes.Add(_redirectUri + "/");
            _listener.Start();
            _logger.LogInformation("Listening for authentication callback...");

            try
            {
                var context = await _listener.GetContextAsync();

                var request = context.Request;
                var response = context.Response;

                string code = request.QueryString["code"];
                if (string.IsNullOrEmpty(code))
                {
                    throw new OAuthException("Authorization failed. No code received.");
                }

                string accessToken = await GetAccessTokenAsync(code);
                var userInfo = await GetUserInfoAsync(accessToken);

                string htmlContent = Helper.GenerateHtmlFromFile(_logger, htmlCallbackPath, userInfo.Username, Helper.GetUserAvatar(userInfo), userInfo.Email, _appName);
                byte[] buffer = Encoding.UTF8.GetBytes(htmlContent);
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                response.OutputStream.Close();

                _listener.Stop();
                _logger.LogInformation("Authentication completed successfully.");

                return userInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Authentication failed: {ex.Message}");
                throw new OAuthException($"Authentication failed: {ex.Message}");
            }
            finally
            {
                _listener?.Stop(); 
            }
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
                _logger.LogError($"Failed to get access token: {responseString}");
                throw new OAuthException("Failed to get access token: " + responseString);
            }

            _logger.LogInformation("Access token received successfully.");
            var json = JsonConvert.DeserializeObject<dynamic>(responseString);
            return json.access_token;
        }

        private async Task<UserInfo> GetUserInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            var response = await client.GetStringAsync("https://discord.com/api/users/@me");
            _logger.LogInformation("User info received successfully.");
            return JsonConvert.DeserializeObject<UserInfo>(response);
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
                _logger.LogError($"Failed to open URL: {ex.Message}");
                Console.WriteLine("Please open the following URL manually: " + url);
            }
        }
    }
}
