namespace Teltonika.Avl.Elements.Beacons;

/// <summary>
/// The decoded contents of a simple-mode beacon element (AVL ID 385).
/// Large scans are split across multiple records; <see cref="CurrentPart"/> of
/// <see cref="TotalParts"/> identifies this record's position in the split.
/// </summary>
public sealed record BeaconList(byte CurrentPart, byte TotalParts, IReadOnlyList<Beacon> Beacons);
