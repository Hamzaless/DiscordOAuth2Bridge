# OAuth2Bridge - Discord OAuth2 Authentication

OAuth2Bridge is a simple and powerful C# library that simplifies OAuth2 authentication with Discord. This library handles user authentication via Discord’s OAuth2 flow, making it easy to authenticate users and retrieve their data.

## Features

- **Discord OAuth2 Authentication**: Allows your application to authenticate users using Discord’s OAuth2 service.
- **Customizable Scopes**: Easily configure the required Discord OAuth2 scopes for your application (e.g., `Identify`, `Email`, etc.).
- **Timeout Handling**: Automatically handles authentication timeouts with a customizable timeout period.
- **User Info Retrieval**: Retrieve user information after authentication, including their username, email, avatar, and more.
- **Easy Integration**: Simple API designed for ease of use with minimal configuration.

## Example

<img src="https://github.com/Hamzaless/OAuth2Bridge/blob/master/oauth3.png?raw=true" width="200" />
<img src="https://github.com/Hamzaless/OAuth2Bridge/blob/master/oauth4.png?raw=true" width="200" />


## Installation

To get started, you can install the OAuth2Bridge library via NuGet:

```
Install-Package OAuth2Bridge
```

## Usage

### 1. **Basic Example**

To authenticate a user, you just need to create an instance of `OAuthServer`, configure your OAuth credentials, and call the `AuthenticateAsync` method.

```csharp
using OAuth2Bridge;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Run the async Auth method
            Auth().Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task Auth()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<OAuthLogger>();

        var oAuthLogger = new OAuthLogger(logger);

        // Create the OAuth server instance
        var server = OAuthServer.CreateServer("YOUR_CLIENT_ID", "YOUR_CLIENT_SECRET", 3465, oAuthLogger, "Your App Name");

        // Add necessary Discord scopes
        server.Scopes.Add(DiscordScopes.Email);
        server.Scopes.Add(DiscordScopes.Identify);

        var cancellationTokenSource = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(10);

        // Handle timeout
        var timeoutTask = Task.Delay(timeout, cancellationTokenSource.Token);

        try
        {
            // Start the authentication process
            var userInfoTask = server.AuthenticateAsync(cancellationTokenSource.Token, timeoutSeconds: (int)timeout.TotalSeconds);

            var completedTask = await Task.WhenAny(userInfoTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                cancellationTokenSource.Cancel();
                Console.WriteLine("Authentication timed out.");
                return;
            }

            // Authentication completed, print user info
            var userInfo = await userInfoTask;
            Console.WriteLine(JsonConvert.SerializeObject(userInfo, Formatting.Indented));
        }
        catch (OAuthException ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Authentication canceled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}
```

### 2. **Customizing Scopes**

You can customize the OAuth2 scopes to request specific permissions from the user. Some commonly used scopes include:

- `Identify` - Access the user's username and avatar.
- `Email` - Access the user's email address.
- `Guilds` - Get the list of guilds the user is a member of.

Here’s how you can add scopes:

```csharp
server.Scopes.Add(DiscordScopes.Guilds);
server.Scopes.Add(DiscordScopes.Email);
```

### 3. **Handling Authentication Timeout**

The library supports handling timeouts. You can specify the timeout period when calling `AuthenticateAsync`. If authentication takes too long, it will be canceled.

```csharp
var userInfo = await server.AuthenticateAsync(cancellationToken, timeoutSeconds: 10);
```

### 4. **Retrieving User Info**

Once authenticated, you can easily retrieve the authenticated user’s information:

```csharp
public class UserInfo
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Avatar { get; set; }
    public string Email { get; set; }
}
```

You can use `JsonConvert.SerializeObject` to display the user's info in a readable format:

```csharp
Console.WriteLine(JsonConvert.SerializeObject(userInfo, Formatting.Indented));
```

## Configuration

You will need to configure the following details to authenticate with Discord’s OAuth2 service:

- `ClientId`: Your Discord application's client ID.
- `ClientSecret`: Your Discord application's client secret.
