using System.Net;

namespace Teltonika.Avl.Server;

public sealed class TeltonikaServerOptions
{
    public IPAddress Address { get; set; } = IPAddress.Any;
    public int Port { get; set; } = 5027;
    public int MaxConnections { get; set; } = 1000;
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public Func<string, CancellationToken, ValueTask<bool>>? ImeiValidator { get; set; }
}
