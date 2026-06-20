using Teltonika.Avl.Models;

namespace Teltonika.Avl.Server;

public sealed class CommandReceivedEventArgs : EventArgs
{
    public required string Imei { get; init; }
    public required GprsCommandPacket Command { get; init; }
}
