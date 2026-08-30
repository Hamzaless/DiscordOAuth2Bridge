using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OAuth2Bridge;

public class UserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("global_name")]
        public string? GlobalName { get; set; }

        [JsonPropertyName("discriminator")]
        public string Discriminator { get; set; } = "0";

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("mfa_enabled")]
        public bool MfaEnabled { get; set; }

        [JsonPropertyName("banner")]
        public string? Banner { get; set; }

        [JsonPropertyName("accent_color")]
        public int? AccentColor { get; set; }
    }

    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("webhook")]
        public WebhookInfo? Webhook { get; set; }

        [JsonPropertyName("guild")]
        public GuildInfo? Guild { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddSeconds(-30);
    }

    public class WebhookInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public int Type { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
        [JsonPropertyName("channel_id")]
        public string ChannelId { get; set; } = string.Empty;
        [JsonPropertyName("guild_id")]
        public string? GuildId { get; set; }
        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; } = string.Empty;
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public class RoleConnectionInfo
    {
        [JsonPropertyName("platform_name")]
        public string? PlatformName { get; set; }
        [JsonPropertyName("platform_username")]
        public string? PlatformUsername { get; set; }
        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class EntitlementInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("sku_id")]
        public string SkuId { get; set; } = string.Empty;
        [JsonPropertyName("application_id")]
        public string ApplicationId { get; set; } = string.Empty;
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }
        [JsonPropertyName("type")]
        public int Type { get; set; }
        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }
        [JsonPropertyName("starts_at")]
        public string? StartsAt { get; set; }
        [JsonPropertyName("ends_at")]
        public string? EndsAt { get; set; }
    }

    public class GuildInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
        [JsonPropertyName("owner")]
        public bool Owner { get; set; }
        [JsonPropertyName("permissions")]
        public string? Permissions { get; set; }
        [JsonPropertyName("features")]
        public List<string> Features { get; set; } = new();
    }

    public class GuildMemberInfo
    {
        [JsonPropertyName("user")]
        public UserInfo? User { get; set; }
        [JsonPropertyName("nick")]
        public string? Nick { get; set; }
        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new();
        [JsonPropertyName("joined_at")]
        public string? JoinedAt { get; set; }
        [JsonPropertyName("deaf")]
        public bool Deaf { get; set; }
        [JsonPropertyName("mute")]
        public bool Mute { get; set; }
    }

    public class ConnectionInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        [JsonPropertyName("visibility")]
        public int Visibility { get; set; }
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }

    public class AuthorizationInfo
    {
        [JsonPropertyName("application")]
        public AuthorizationApp? Application { get; set; }
        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; } = new();
        [JsonPropertyName("expires")]
        public string? Expires { get; set; }
        [JsonPropertyName("user")]
        public UserInfo? User { get; set; }
    }

    public class AuthorizationApp
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
    }