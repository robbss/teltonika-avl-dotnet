namespace Teltonika.Avl.Models;

public sealed record IoElement(
    ushort EventId,
    IReadOnlyList<IoProperty> Properties);
