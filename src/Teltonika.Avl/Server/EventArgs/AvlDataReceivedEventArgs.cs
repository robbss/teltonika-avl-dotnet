using Teltonika.Avl.Models;

namespace Teltonika.Avl.Server;

public sealed class AvlDataReceivedEventArgs : EventArgs
{
    public required string Imei { get; init; }
    public required AvlPacket Packet { get; init; }
}
