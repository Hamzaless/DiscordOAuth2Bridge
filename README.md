# OAuth2Bridge - Discord OAuth2 Authentication

[![NuGet](https://img.shields.io/nuget/v/OAuth2Bridge.svg)](https://www.nuget.org/packages/OAuth2Bridge/) [![NuGet Downloads](https://img.shields.io/nuget/dt/OAuth2Bridge.svg)](https://www.nuget.org/packages/OAuth2Bridge/) [![Build](https://github.com/Hamzaless/DiscordOAuth2Bridge/actions/workflows/ci.yml/badge.svg)](https://github.com/Hamzaless/DiscordOAuth2Bridge/actions)

[NUGET PACKAGE](https://www.nuget.org/packages/OAuth2Bridge/)

OAuth2Bridge is a simple and powerful C# library that simplifies OAuth2 authentication with Discord. This library handles user authentication via Discord's OAuth2 flow, making it easy to authenticate users and retrieve their data.

## Features

- Discord OAuth2 with PKCE S256, refresh/revoke and auto free port
- User data: guilds, member roles, connections, entitlements, webhooks
- Secure by default: state/CSRF, file-based HTML templates
- Works with .NET 8 and .NET Framework 4.7.2, with or without DI

## Example

<img src="https://github.com/Hamzaless/OAuth2Bridge/blob/master/oauth3.png?raw=true" width="600" />
<img src="https://github.com/Hamzaless/OAuth2Bridge/blob/master/oauth4.png?raw=true" width="600" />

## Installation

```
Install-Package OAuth2Bridge
```

## Usage

### 1. Basic Example

```csharp
using OAuth2Bridge;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<OAuthLogger>();

        using var server = OAuthServer.CreateServer("YOUR_CLIENT_ID", "YOUR_CLIENT_SECRET", 3465, new OAuthLogger(logger), "Your App Name");
        server.Scopes.Add(DiscordScopes.Identify);
        server.Scopes.Add(DiscordScopes.Email);

        try
        {
            var user = await server.AuthenticateAsync(cts.Token, "./data/success.html", "./data/error.html");
            Console.WriteLine(user.Username);
            Console.WriteLine(Helper.GetUserAvatar(user));
        }
        catch (OAuthException ex)
        {
            Console.WriteLine($"Auth failed: {ex.Message}");
        }
    }
}
```

### 2. Scopes and Advanced

```csharp
server.Scopes.Add(DiscordScopes.GuildsJoin); // -> "guilds.join"

var (user, token) = await server.AuthenticateWithTokenAsync(cts.Token);
var guilds = await server.GetUserGuildsAsync(token.AccessToken);
var member = await server.GetGuildMemberAsync(token.AccessToken, "123456789012345678");

var opts = new OAuthServerOptions
{
    ClientId = "YOUR_CLIENT_ID",
    ClientSecret = null, // public client with PKCE
    Port = 0,
    EnablePkce = true,
    SuccessHtmlPath = "./data/success.html",
    ErrorHtmlPath = "./data/error.html"
};
```

For `net472` use `MainAsync().GetAwaiter().GetResult()` instead of `async Main`.

## Configuration

You need from https://discord.com/developers/applications:
- `ClientId`
- `ClientSecret` (only for confidential clients, otherwise `null` with `EnablePkce = true`)
- Redirect URI: `http://localhost:{port}/callback`

> Don't hardcode secrets. Use `dotnet user-secrets` or env vars.

## HTML Templates

Success: `%app_name%`, `%profile_name%`, `%profile_url%`, `%profile_email%`
Error: `%app_name%`, `%error%`
See `data/success.html` and `data/error.html`.

## License

MIT - see [LICENSE](LICENSE)

### Changelog

**2.0.0**
- `net6.0` -> `net8.0;net472`, PKCE S256, refresh/revoke, guilds/connections/entitlements/webhooks, linked roles
- Fixed scope mapping, added state/CSRF, file-based HTML, secure secret handling, DI support
