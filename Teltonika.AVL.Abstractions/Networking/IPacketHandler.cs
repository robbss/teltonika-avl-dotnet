using Teltonika.AVL.Models;

namespace Teltonika.AVL.Networking;

public interface IPacketHandler
{
    Task HandlePacketAsync(string imei, AvlDataPacket packet, CancellationToken cancellationToken);
}