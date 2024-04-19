namespace Teltonika.AVL;

public class AvlIOElement
{
    public short Id { get; set; }

    public byte[] Value { get; set; } = default!;
}