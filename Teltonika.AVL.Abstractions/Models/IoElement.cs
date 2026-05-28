namespace Teltonika.AVL.Models;

public class IoElement
{
    public int RawId { get; init; }
    public IoType Type { get; init; }
    public object Value { get; init; } = null!;
    public IoUnit Unit { get; init; }
}