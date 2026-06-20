namespace Teltonika.Avl.Server;

public sealed class DeviceConnectedEventArgs : EventArgs
{
    public required string Imei { get; init; }
    public required System.Net.IPEndPoint RemoteEndPoint { get; init; }
    public required DateTimeOffset ConnectedAt { get; init; }
}
