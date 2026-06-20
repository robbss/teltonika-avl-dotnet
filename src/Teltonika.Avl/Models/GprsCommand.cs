namespace Teltonika.Avl.Models;

public sealed record GprsCommand(string CommandText);

public sealed record GprsCommandWithImei(string Imei, string CommandText);

public sealed record GprsCommandResponse(CodecId CodecId, string ResponseText);

public sealed record GprsCommandPacket(
    CodecId CodecId,
    byte CommandType,
    string CommandText,
    string? Imei = null,
    DateTimeOffset? Timestamp = null);
