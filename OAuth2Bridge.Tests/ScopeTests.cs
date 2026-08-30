using OAuth2Bridge;
using Xunit;

public class ScopeTests
{
    [Fact]
    public void GuildsJoin_MapsTo_DottedString()
    {
        Assert.Equal("guilds.join", DiscordScopes.GuildsJoin.ToScopeString());
    }

    [Fact]
    public void GuildsMembersRead_MapsCorrectly()
    {
        Assert.Equal("guilds.members.read", DiscordScopes.GuildsMembersRead.ToScopeString());
    }

    [Fact]
    public void RoleConnectionsWrite_MapsCorrectly()
    {
        Assert.Equal("role_connections.write", DiscordScopes.RoleConnectionsWrite.ToScopeString());
    }

    [Fact]
    public void ToScopeParam_JoinsWithSpace()
    {
        var scopes = new[] { DiscordScopes.Identify, DiscordScopes.Email, DiscordScopes.GuildsJoin };
        Assert.Equal("identify email guilds.join", scopes.ToScopeParam());
    }

    [Fact]
    public void AllScopes_NoTurkish_I_Bug()
    {
        foreach (DiscordScopes s in Enum.GetValues(typeof(DiscordScopes)))
        {
            var str = s.ToScopeString();
            Assert.DoesNotContain("ı", str);
            Assert.Equal(str, str.ToLowerInvariant());
        }
    }
}