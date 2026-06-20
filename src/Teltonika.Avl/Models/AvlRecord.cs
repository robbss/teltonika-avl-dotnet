namespace Teltonika.Avl.Models;

public sealed record AvlRecord(
    DateTimeOffset Timestamp,
    Priority Priority,
    GpsData Gps,
    IoElement IoData);
