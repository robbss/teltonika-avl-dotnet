namespace Teltonika.Avl.Models;

/// <summary>The I/O element of a record: what caused it, and the values riding along.</summary>
/// <param name="EventId">AVL id of the element that caused the record, or 0 for a periodic one.</param>
/// <param name="Properties">The values carried, in wire order.</param>
/// <param name="GenerationType">
/// Codec 16 only: why the record was generated. Carried on the model so a Codec 16 packet can be
/// re-encoded byte-for-byte; Codec 8 and 8 Extended have no such field and leave it at zero.
/// </param>
public sealed record IoElement(
    ushort EventId,
    IReadOnlyList<IoProperty> Properties,
    byte GenerationType = 0);
