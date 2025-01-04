# Usage

```

private async static Task auth()
{
    OAuthLogger logger = new OAuthLogger();
    logger.OnLog += (message) => Console.WriteLine(message);
    var server = OAuthServer.CreateServer("application-id", "application-secret", 3465, logger, "Application Name");
    server.Scopes.Add(DiscordScopes.Email);
    server.Scopes.Add(DiscordScopes.Identify);
    try
    {
        var userInfo = await server.AuthenticateAsync();
        Console.WriteLine(JsonConvert.SerializeObject(userInfo, Formatting.Indented));
    }
    catch (Exception ex)
    {
        Console.WriteLine("Authentication failed: " + ex.Message);
    }
    Console.ReadKey();
}

```
