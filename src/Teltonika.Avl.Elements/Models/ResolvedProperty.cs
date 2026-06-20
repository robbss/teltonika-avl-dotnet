namespace Teltonika.Avl.Elements.Models;

public readonly record struct ResolvedProperty(
    ushort Id,
    string Name,
    string? Group,
    object Value,
    string? Units,
    string? Description);
