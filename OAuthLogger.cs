using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge;

public class OAuthLogger
    {
        private readonly ILogger _logger;

        public OAuthLogger(ILogger<OAuthLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public OAuthLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogInformation(string message) => _logger.LogInformation(message);
        public void LogError(string message) => _logger.LogError(message);
        public void LogWarning(string message) => _logger.LogWarning(message);
        public void LogDebug(string message) => _logger.LogDebug(message);
    }