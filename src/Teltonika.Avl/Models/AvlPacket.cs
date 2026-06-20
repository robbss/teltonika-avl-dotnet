namespace Teltonika.Avl.Models;

public sealed record AvlPacket(
    CodecId CodecId,
    IReadOnlyList<AvlRecord> Records);
