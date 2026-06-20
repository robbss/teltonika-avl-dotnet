using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teltonika.Avl.Server;

namespace Teltonika.Avl.AspNetCore;

internal sealed class TeltonikaHostedService(
    IOptions<TeltonikaServerOptions> options,
    IServiceProvider serviceProvider,
    TeltonikaServerAccessor accessor,
    ILogger<TeltonikaHostedService> logger) : BackgroundService
{
    private TeltonikaServer? _server;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serverOptions = options.Value;
        serverOptions.ImeiValidator = ValidateImeiAsync;

        _server = new TeltonikaServer(serverOptions);
        accessor.Set(_server);

        _server.DeviceConnected += (_, e) => DispatchAsync(
            (h, ct) => h.OnDeviceConnectedAsync(e, ct), stoppingToken);
        _server.AvlDataReceived += (_, e) => DispatchAsync(
            (h, ct) => h.OnAvlDataReceivedAsync(e, ct), stoppingToken);
        _server.DeviceDisconnected += (_, e) => DispatchAsync(
            (h, ct) => h.OnDeviceDisconnectedAsync(e, ct), stoppingToken);
        _server.CommandReceived += (_, e) => DispatchAsync(
            (h, ct) => h.OnCommandReceivedAsync(e, ct), stoppingToken);

        logger.LogInformation("Teltonika AVL server starting on {Address}:{Port}",
            serverOptions.Address, serverOptions.Port);

        await _server.StartAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_server is not null)
        {
            logger.LogInformation("Teltonika AVL server stopping");
            await _server.StopAsync(cancellationToken);
            await _server.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task DispatchAsync(
        Func<ITeltonikaDeviceHandler, CancellationToken, Task> action,
        CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<ITeltonikaDeviceHandler>();

        foreach (var handler in handlers)
        {
            try
            {
                await action(handler, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Teltonika device handler {Handler} threw an exception",
                    handler.GetType().Name);
            }
        }
    }

    private async ValueTask<bool> ValidateImeiAsync(string imei, CancellationToken ct)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<ITeltonikaDeviceHandler>();

        foreach (var handler in handlers)
        {
            if (!await handler.ValidateImeiAsync(imei, ct))
                return false;
        }

        return true;
    }
}
