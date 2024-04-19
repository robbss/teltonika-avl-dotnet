namespace Teltonika.AVL;

public class AvlElementInfo
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required Type Type { get; init; }

    public double? Multiplier { get; init; }

    public string? Unit { get; set; }
}