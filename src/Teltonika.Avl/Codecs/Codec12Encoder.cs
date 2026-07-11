using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec12Encoder : ICommandCodecEncoder
{
    public static readonly Codec12Encoder Instance = new();

    public byte[] EncodeCommandPacket(GprsCommandPacket command)
    {
        int dataLength = 8 + command.CommandText.Length; // codec + qty + type + size + command + qty
        var buffer = PacketFramer.AllocatePacket(dataLength);
        var writer = new SpanWriter(buffer.AsSpan(8, dataLength));

        writer.WriteByte(0x0C);
        writer.WriteByte(0x01);
        writer.WriteByte(command.CommandType);
        writer.WriteInt32(command.CommandText.Length);
        writer.WriteAscii(command.CommandText);
        writer.WriteByte(0x01);

        PacketFramer.SealPacket(buffer);
        return buffer;
    }
}
