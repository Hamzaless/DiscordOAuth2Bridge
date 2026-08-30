using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OAuth2Bridge;

public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscordOAuth2Bridge(this IServiceCollection services, Action<OAuthServerOptions> configure)
        {
            var opts = new OAuthServerOptions();
            configure(opts);
            services.AddSingleton(opts);
            services.AddSingleton(sp =>
            {
                var logger = sp.GetService<ILogger<OAuthLogger>>();
                var oAuthLogger = logger != null ? new OAuthLogger(logger) : new OAuthLogger(Microsoft.Extensions.Logging.Abstractions.NullLogger<OAuthLogger>.Instance);
                var server = new OAuthServer(opts.ClientId, opts.ClientSecret, opts.Port, oAuthLogger, opts.AppName);
                foreach (var s in opts.Scopes) server.Scopes.Add(s);
                foreach (var kv in opts.AdditionalAuthParams) server.AdditionalAuthParams[kv.Key] = kv.Value;
                server.EnablePkce = opts.EnablePkce;
                server.Prompt = opts.Prompt;
                server.SuccessHtmlPath = opts.SuccessHtmlPath;
                server.ErrorHtmlPath = opts.ErrorHtmlPath;
                return server;
            });
            return services;
        }

        public static IServiceCollection AddDiscordOAuth2Bridge(this IServiceCollection services, OAuthServerOptions options)
            => services.AddDiscordOAuth2Bridge(o => { o.ClientId = options.ClientId; o.ClientSecret = options.ClientSecret; o.Port = options.Port; o.AppName = options.AppName; o.Scopes = options.Scopes; o.Timeout = options.Timeout; o.EnablePkce = options.EnablePkce; });
    }