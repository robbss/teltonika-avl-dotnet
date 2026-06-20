using Teltonika.Avl.Server;

namespace Teltonika.Avl.AspNetCore;

public interface ITeltonikaServerAccessor
{
    TeltonikaServer Server { get; }
}
