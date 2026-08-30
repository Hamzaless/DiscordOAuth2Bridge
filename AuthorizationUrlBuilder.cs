using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OAuth2Bridge;

internal static class AuthorizationUrlBuilder
{
    public static string Build(string clientId, string redirectUri, IReadOnlyList<DiscordScopes> scopes, string state, string prompt, bool enablePkce, string? codeChallenge, Dictionary<string,string>? extra)
    {
        var sb = new StringBuilder($"https://discord.com/api/oauth2/authorize?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(scopes.ToScopeParam())}&state={Uri.EscapeDataString(state)}&prompt={Uri.EscapeDataString(prompt)}");
        if (enablePkce && codeChallenge != null)
            sb.Append($"&code_challenge={Uri.EscapeDataString(codeChallenge)}&code_challenge_method=S256");
        if (extra != null)
            foreach (var kv in extra)
                sb.Append($"&{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}");
        return sb.ToString();
    }

    public static string BuildBotInvite(string clientId, long permissions = 0, string? guildId = null, bool disableGuildSelect = false, string? redirectUri = null, string[]? scopes = null)
    {
        var sc = scopes != null && scopes.Length > 0 ? string.Join(" ", scopes) : "bot";
        var url = new StringBuilder($"https://discord.com/api/oauth2/authorize?client_id={Uri.EscapeDataString(clientId)}&scope={Uri.EscapeDataString(sc)}");
        if (permissions != 0) url.Append($"&permissions={permissions}");
        if (!string.IsNullOrEmpty(guildId)) url.Append($"&guild_id={Uri.EscapeDataString(guildId)}");
        if (disableGuildSelect) url.Append("&disable_guild_select=true");
        if (!string.IsNullOrEmpty(redirectUri)) url.Append($"&redirect_uri={Uri.EscapeDataString(redirectUri)}");
        return url.ToString();
    }
}