using Microsoft.Extensions.Hosting;
using Teltonika.AVL.Networking;

namespace Teltonika.AVL.AspNetCore;

public class TeltonikaTcpHostedService : BackgroundService
{
    private readonly TcpPipelineServer _server;

    public TeltonikaTcpHostedService(TcpPipelineServer server)
    {
        _server = server;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _server.StartAsync(stoppingToken);
    }
}