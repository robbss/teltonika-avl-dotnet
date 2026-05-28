namespace Teltonika.AVL.Models;

public class AvlDataPacket
{
    public AvlCodec CodecId { get; init; }
    public int RecordCount { get; init; }
    public IReadOnlyList<AvlRecord> Records { get; init; } = null!;
}