namespace Teltonika.AVL.Models;

public class AvlMessage
{
    public required AvlCodec Codec { get; set; }

    public required AvlRecord[] Records { get; set; }
}