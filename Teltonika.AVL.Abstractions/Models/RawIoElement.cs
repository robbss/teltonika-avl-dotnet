namespace Teltonika.AVL.Models;

public class RawIoElement
{
    public int EventId { get; init; }
    public int TotalIoCount { get; init; }
    public Dictionary<int, byte[]> Properties { get; init; } = new();
}