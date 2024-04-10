namespace Teltonika.AVL;

public class AvlHeader
{
    public int Preamble { get; init; }

    public int DataLength { get; init; }

    public byte CodecId { get; init; }

    public byte NumberOfData { get; init; }
}