using System;
using System.Collections.Generic;

namespace OAuth2Bridge;

public class OAuthServerOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }

        public int Port { get; set; } = 5000;

        public string AppName { get; set; } = "OAuth2Bridge";

        public List<DiscordScopes> Scopes { get; set; } = new();

        public TimeSpan? Timeout { get; set; }

        public bool EnablePkce { get; set; } = false;

        public string Prompt { get; set; } = "consent";

        public Dictionary<string, string> AdditionalAuthParams { get; set; } = new();

        public string? SuccessHtmlPath { get; set; } = "./data/success.html";

        public string? ErrorHtmlPath { get; set; }
    }