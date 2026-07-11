namespace Teltonika.Avl.Elements.Beacons;

/// <summary>
/// Flags byte preceding each record in the simple beacon list (AVL ID 385).
/// A cleared <see cref="IBeacon"/> bit means the record is an Eddystone beacon.
/// </summary>
[Flags]
public enum BeaconFlags : byte
{
    None = 0,
    Rssi = 0x01,
    BatteryVoltage = 0x02,
    Temperature = 0x04,
    IBeacon = 0x20,
}
