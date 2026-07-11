namespace Teltonika.Avl.Elements.Beacons;

/// <summary>
/// A single beacon reported in advanced beacon mode (AVL ID 548). The ID and
/// additional data layout depend on the device's beacon capturing configuration.
/// </summary>
public sealed record AdvancedBeacon(sbyte? Rssi, ReadOnlyMemory<byte> BeaconId, ReadOnlyMemory<byte> AdditionalData);
