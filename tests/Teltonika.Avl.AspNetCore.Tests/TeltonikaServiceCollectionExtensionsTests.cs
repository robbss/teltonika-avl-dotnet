using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Teltonika.Avl.AspNetCore;
using Teltonika.Avl.Models;
using Teltonika.Avl.Server;

namespace Teltonika.Avl.AspNetCore.Tests;

public class TeltonikaServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTeltonikaServer_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTeltonikaServer(o => o.Port = 9999);

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ITeltonikaServerAccessor>());
        Assert.NotNull(provider.GetServices<IHostedService>()
            .SingleOrDefault(s => s.GetType().Name == "TeltonikaHostedService"));
    }

    [Fact]
    public void AddTeltonikaServer_WithConfiguration_BindsValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TeltonikaServer:Address"] = "127.0.0.1",
                ["TeltonikaServer:Port"] = "6789",
                ["TeltonikaServer:MaxConnections"] = "42",
                ["TeltonikaServer:IdleTimeoutSeconds"] = "120",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTeltonikaServer(config.GetSection("TeltonikaServer"));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TeltonikaServerOptions>>();

        Assert.Equal(IPAddress.Loopback, options.Value.Address);
        Assert.Equal(6789, options.Value.Port);
        Assert.Equal(42, options.Value.MaxConnections);
        Assert.Equal(TimeSpan.FromSeconds(120), options.Value.IdleTimeout);
    }

    [Fact]
    public void AddTeltonikaHandler_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTeltonikaServer();
        services.AddTeltonikaHandler<TestHandler>();

        var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<ITeltonikaDeviceHandler>().ToList();

        Assert.Single(handlers);
        Assert.IsType<TestHandler>(handlers[0]);
    }

    [Fact]
    public void AddTeltonikaHandler_MultipleHandlers_AllRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTeltonikaServer();
        services.AddTeltonikaHandler<TestHandler>();
        services.AddTeltonikaHandler<SecondHandler>();

        var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<ITeltonikaDeviceHandler>().ToList();

        Assert.Equal(2, handlers.Count);
    }

    private sealed class TestHandler : ITeltonikaDeviceHandler { }
    private sealed class SecondHandler : ITeltonikaDeviceHandler { }
}
