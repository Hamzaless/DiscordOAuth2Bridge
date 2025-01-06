using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge
{
    public static class Helper
    {
        public static string GetUserAvatar(UserInfo userInfo)
        {
            return $"https://cdn.discordapp.com/avatars/{userInfo.Id}/{userInfo.Avatar}";
        }
        public static string GenerateHtmlFromFile(OAuthLogger _logger, string filePath = null, string profileName = "", string profileUrl = "", string profileEmail = "", string appName = "OAuth2Bridge" )
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
                .Replace("%app_name%", appName)
                .Replace("%profile_name%", profileName)
                .Replace("%profile_url%", profileUrl)
                .Replace("%profile_email%", profileEmail);

            return htmlContent;
        }
    }
}
