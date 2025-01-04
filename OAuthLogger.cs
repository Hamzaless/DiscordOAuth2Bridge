using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge
{
    /// <summary>
    /// OAuthLogger class for logging events related to OAuth operations.
    /// </summary>
    public class OAuthLogger
    {
        private readonly ILogger<OAuthLogger> _logger;

        public OAuthLogger(ILogger<OAuthLogger> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message) => _logger.LogInformation(message);
        public void LogError(string message) => _logger.LogError(message);
    }
}
