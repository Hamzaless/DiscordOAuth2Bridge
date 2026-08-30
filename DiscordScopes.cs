using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge;

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

    public static class DiscordScopesExtensions
    {
        private static readonly Dictionary<DiscordScopes, string> ScopeMap = new()
        {
            { DiscordScopes.Identify, "identify" },
            { DiscordScopes.Email, "email" },
            { DiscordScopes.Connections, "connections" },
            { DiscordScopes.Guilds, "guilds" },
            { DiscordScopes.GuildsJoin, "guilds.join" },
            { DiscordScopes.GuildsMembersRead, "guilds.members.read" },
            { DiscordScopes.MessagesRead, "messages.read" },
            { DiscordScopes.RelationshipsRead, "relationships.read" },
            { DiscordScopes.ActivitiesRead, "activities.read" },
            { DiscordScopes.ActivitiesWrite, "activities.write" },
            { DiscordScopes.ApplicationsBuildsRead, "applications.builds.read" },
            { DiscordScopes.ApplicationsBuildsUpload, "applications.builds.upload" },
            { DiscordScopes.ApplicationsCommands, "applications.commands" },
            { DiscordScopes.ApplicationsCommandsUpdate, "applications.commands.update" },
            { DiscordScopes.ApplicationsEntitlements, "applications.entitlements" },
            { DiscordScopes.ApplicationsStoreUpdate, "applications.store.update" },
            { DiscordScopes.Bot, "bot" },
            { DiscordScopes.WebhookIncoming, "webhook.incoming" },
            { DiscordScopes.Rpc, "rpc" },
            { DiscordScopes.RpcNotificationsRead, "rpc.notifications.read" },
            { DiscordScopes.RpcVoiceWrite, "rpc.voice.write" },
            { DiscordScopes.RpcVoiceRead, "rpc.voice.read" },
            { DiscordScopes.RpcVideoWrite, "rpc.voice.write" },
            { DiscordScopes.RpcVideoRead, "rpc.voice.read" },
            { DiscordScopes.RpcScreenshareRead, "rpc.screenshare.read" },
            { DiscordScopes.RpcScreenshareWrite, "rpc.screenshare.write" },
            { DiscordScopes.RoleConnectionsWrite, "role_connections.write" },
            { DiscordScopes.Voice, "voice" },
            { DiscordScopes.PresencesRead, "presences.read" },
            { DiscordScopes.PresencesWrite, "presences.write" },
            { DiscordScopes.DMChannelsRead, "dm_channels.read" },
            { DiscordScopes.DMChannelsMessagesWrite, "dm_channels.messages.write" },
            { DiscordScopes.PaymentSourcesCountryCode, "payment_sources.country_code" },
            { DiscordScopes.OpenID, "openid" },
            { DiscordScopes.GatewayConnect, "gdm.join" },
            { DiscordScopes.SDKSocialLayer, "sdk.social_layer" },
            { DiscordScopes.AccountGlobalNameUpdate, "account.global_name.update" },
        };

        public static string ToScopeString(this DiscordScopes scope)
        {
            if (ScopeMap.TryGetValue(scope, out var mapped))
                return mapped;
            return scope.ToString().ToLowerInvariant();
        }

        public static string ToScopeParam(this IEnumerable<DiscordScopes> scopes)
        {
            return string.Join(" ", scopes.Select(s => s.ToScopeString()));
        }
    }