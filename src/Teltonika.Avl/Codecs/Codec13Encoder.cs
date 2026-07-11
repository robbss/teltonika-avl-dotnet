using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec13Encoder : ICommandCodecEncoder
{
    public static readonly Codec13Encoder Instance = new();

    public byte[] EncodeCommandPacket(GprsCommandPacket command)
    {
        int timestampSeconds = command.Timestamp.HasValue
            ? (int)command.Timestamp.Value.ToUnixTimeSeconds()
            : (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        int dataLength = 12 + command.CommandText.Length; // codec + qty + type + timestamp + size + command + qty
        var buffer = PacketFramer.AllocatePacket(dataLength);
        var writer = new SpanWriter(buffer.AsSpan(8, dataLength));

        writer.WriteByte(0x0D);
        writer.WriteByte(0x01);
        writer.WriteByte(command.CommandType);
        writer.WriteInt32(timestampSeconds);
        writer.WriteInt32(command.CommandText.Length);
        writer.WriteAscii(command.CommandText);
        writer.WriteByte(0x01);

        PacketFramer.SealPacket(buffer);
        return buffer;
    }
}
