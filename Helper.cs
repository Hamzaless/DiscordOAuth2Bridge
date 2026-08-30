using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge;

public static class Helper
{
        public static string GetUserAvatar(UserInfo userInfo)
        {
            if (userInfo == null) throw new ArgumentNullException(nameof(userInfo));

            if (string.IsNullOrEmpty(userInfo.Avatar))
            {
                if (long.TryParse(userInfo.Id, out var id))
                    return $"https://cdn.discordapp.com/embed/avatars/{id % 6}.png";
                return "https://cdn.discordapp.com/embed/avatars/0.png";
            }

            string extension = userInfo.Avatar.StartsWith("a_") ? "gif" : "png";
            return $"https://cdn.discordapp.com/avatars/{userInfo.Id}/{userInfo.Avatar}.{extension}?size=256";
        }

        public static string GenerateHtmlFromFile(OAuthLogger _logger, string? filePath = null, string profileName = "", string profileUrl = "", string profileEmail = "", string appName = "OAuth2Bridge")
        {
            string htmlContent;

            if (string.IsNullOrEmpty(filePath))
            {
                htmlContent = @"
                    <html>
                        <head><title>%app_name% - Authentication</title></head>
                        <body>
                            <h1>Welcome, %profile_name%!</h1>
                            <img src='%profile_url%' alt='%profile_name%' />
                            <p>Email: %profile_email%</p>
                            <p>Thank you for authenticating with %app_name%!</p>
                        </body>
                    </html>";
            }
            else
            {
                try
                {
                    htmlContent = File.ReadAllText(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading HTML file: {ex.Message}");
                    throw new OAuthException("Failed to read HTML template.");
                }
            }

            htmlContent = htmlContent
                .Replace("%app_name%", WebUtility.HtmlEncode(appName))
                .Replace("%profile_name%", WebUtility.HtmlEncode(profileName))
                .Replace("%profile_url%", WebUtility.HtmlEncode(profileUrl))
                .Replace("%profile_email%", WebUtility.HtmlEncode(profileEmail));

            return htmlContent;
        }

        public static string GetAvatarUrl(this UserInfo user) => GetUserAvatar(user);

        public static string GenerateErrorHtml(OAuthLogger logger, string? filePath, string errorMessage, string appName = "OAuth2Bridge")
        {
            string html;
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try { html = File.ReadAllText(filePath); }
                catch (Exception ex) { logger.LogError($"Error reading error HTML: {ex.Message}"); html = "<html><body><h1>Error</h1><p>%error%</p></body></html>"; }
            }
            else
            {
                html = "<html><body><h1>Error</h1><p>%error%</p></body></html>";
            }
            html = html.Replace("%error%", WebUtility.HtmlEncode(errorMessage)).Replace("%app_name%", WebUtility.HtmlEncode(appName));
            return html;
        }
    }