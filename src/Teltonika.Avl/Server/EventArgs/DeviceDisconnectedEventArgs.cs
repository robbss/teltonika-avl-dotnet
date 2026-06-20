namespace Teltonika.Avl.Server;

public sealed class DeviceDisconnectedEventArgs : EventArgs
{
    public required string Imei { get; init; }
    public required string Reason { get; init; }
}
