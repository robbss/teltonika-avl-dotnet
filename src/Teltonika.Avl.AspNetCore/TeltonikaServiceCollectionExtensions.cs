using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Teltonika.Avl.Server;

namespace Teltonika.Avl.AspNetCore;

public static class TeltonikaServiceCollectionExtensions
{
    public static IServiceCollection AddTeltonikaServer(this IServiceCollection services)
    {
        return services.AddTeltonikaServer(_ => { });
    }

    public static IServiceCollection AddTeltonikaServer(
        this IServiceCollection services,
        Action<TeltonikaServerOptions> configure)
    {
        services.Configure(configure);
        RegisterCoreServices(services);
        return services;
    }

    public static IServiceCollection AddTeltonikaServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TeltonikaServerOptions>(options =>
        {
            var address = configuration["Address"];
            if (!string.IsNullOrEmpty(address))
                options.Address = IPAddress.Parse(address);

            var port = configuration["Port"];
            if (!string.IsNullOrEmpty(port))
                options.Port = int.Parse(port);

            var maxConnections = configuration["MaxConnections"];
            if (!string.IsNullOrEmpty(maxConnections))
                options.MaxConnections = int.Parse(maxConnections);

            var idleTimeoutSeconds = configuration["IdleTimeoutSeconds"];
            if (!string.IsNullOrEmpty(idleTimeoutSeconds))
                options.IdleTimeout = TimeSpan.FromSeconds(int.Parse(idleTimeoutSeconds));
        });

        RegisterCoreServices(services);
        return services;
    }

    public static IServiceCollection AddTeltonikaHandler<T>(this IServiceCollection services)
        where T : class, ITeltonikaDeviceHandler
    {
        services.AddTransient<ITeltonikaDeviceHandler, T>();
        return services;
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        var accessor = new TeltonikaServerAccessor();
        services.TryAddSingleton(accessor);
        services.TryAddSingleton<ITeltonikaServerAccessor>(accessor);
        services.AddHostedService<TeltonikaHostedService>();
    }
}
