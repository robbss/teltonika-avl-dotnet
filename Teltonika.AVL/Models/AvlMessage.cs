namespace Teltonika.AVL.Models;

public class AvlMessage
{
    public required AvlHeader Header { get; set; }

    public required AvlRecord[] Records { get; set; }
}