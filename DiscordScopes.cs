using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Bridge
{
     /// <summary>
     /// Enum representing the various Discord OAuth2 scopes.
     /// </summary>
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
}
