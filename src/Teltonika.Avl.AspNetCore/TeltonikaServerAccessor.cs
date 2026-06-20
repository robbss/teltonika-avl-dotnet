using Teltonika.Avl.Server;

namespace Teltonika.Avl.AspNetCore;

internal sealed class TeltonikaServerAccessor : ITeltonikaServerAccessor
{
    private TeltonikaServer? _server;

    public TeltonikaServer Server => _server ?? throw new InvalidOperationException(
        "The Teltonika server has not been started yet. Ensure the hosted service is running.");

    internal void Set(TeltonikaServer server) => _server = server;
}
