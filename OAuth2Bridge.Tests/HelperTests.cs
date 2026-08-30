using OAuth2Bridge;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class HelperTests
{
    private readonly OAuthLogger _logger = new(NullLogger<OAuthLogger>.Instance);

    [Fact]
    public void GetUserAvatar_NullAvatar_ReturnsDefault()
    {
        var user = new UserInfo { Id = "123456789012345678", Avatar = null, Username = "test" };
        var url = Helper.GetUserAvatar(user);
        Assert.Contains("embed/avatars", url);
        Assert.EndsWith(".png", url);
    }

    [Fact]
    public void GetUserAvatar_Animated_ReturnsGif()
    {
        var user = new UserInfo { Id = "123", Avatar = "a_abc123", Username = "test" };
        var url = Helper.GetUserAvatar(user);
        Assert.Contains(".gif", url);
        Assert.Contains("a_abc123", url);
    }

    [Fact]
    public void GetUserAvatar_Static_ReturnsPng()
    {
        var user = new UserInfo { Id = "123", Avatar = "abc123", Username = "test" };
        var url = Helper.GetUserAvatar(user);
        Assert.Contains(".png", url);
    }

    [Fact]
    public void GenerateHtmlFromFile_XSS_Encoded()
    {
        var html = Helper.GenerateHtmlFromFile(_logger, null, "<script>alert(1)</script>", "https://example.com/a.png", "test@test.com", "MyApp");
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void GenerateHtmlFromFile_ReplacesPlaceholders()
    {
        var html = Helper.GenerateHtmlFromFile(_logger, null, "Hamza", "https://cdn.example.com/avatar.png", "hamza@test.com", "TestApp");
        Assert.Contains("Hamza", html);
        Assert.Contains("TestApp", html);
    }
}