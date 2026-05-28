namespace Teltonika.AVL.Models;

public class AvlRecord
{
    public DateTimeOffset Timestamp { get; init; }
    public int Priority { get; init; }
    public GpsElement Gps { get; init; } = null!;
    public RawIoElement Io { get; init; } = null!;
}