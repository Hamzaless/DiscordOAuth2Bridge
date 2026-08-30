using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OAuth2Bridge;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(UserInfo))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(GuildInfo))]
[JsonSerializable(typeof(List<GuildInfo>))]
[JsonSerializable(typeof(GuildMemberInfo))]
[JsonSerializable(typeof(ConnectionInfo))]
[JsonSerializable(typeof(List<ConnectionInfo>))]
[JsonSerializable(typeof(AuthorizationInfo))]
[JsonSerializable(typeof(RoleConnectionInfo))]
[JsonSerializable(typeof(EntitlementInfo))]
[JsonSerializable(typeof(List<EntitlementInfo>))]
[JsonSerializable(typeof(WebhookInfo))]
internal partial class DiscordJsonContext : JsonSerializerContext { }