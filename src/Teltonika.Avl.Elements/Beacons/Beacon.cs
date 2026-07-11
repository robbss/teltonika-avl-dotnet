namespace Teltonika.Avl.Elements.Beacons;

/// <summary>
/// A single beacon reported in the simple beacon list (AVL ID 385).
/// </summary>
public abstract record Beacon
{
    /// <summary>Received signal strength in dBm, when present.</summary>
    public sbyte? Rssi { get; init; }

    /// <summary>Beacon battery voltage in millivolts, when present.</summary>
    public ushort? BatteryVoltage { get; init; }

    /// <summary>Beacon temperature in degrees Celsius, when present.</summary>
    public short? Temperature { get; init; }
}

/// <summary>An iBeacon advertisement: 16-byte UUID, major and minor identifiers.</summary>
public sealed record IBeacon(Guid Uuid, ushort Major, ushort Minor) : Beacon;

/// <summary>An Eddystone advertisement: 10-byte namespace and 6-byte instance ID.</summary>
public sealed record Eddystone(ReadOnlyMemory<byte> Namespace, ReadOnlyMemory<byte> InstanceId) : Beacon;
