namespace Teltonika.Avl.Models;

public sealed record GprsCommandPacket(
    CodecId CodecId,
    byte CommandType,
    string CommandText,
    string? Imei = null,
    DateTimeOffset? Timestamp = null);
