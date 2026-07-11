using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec14Encoder : ICommandCodecEncoder
{
    public static readonly Codec14Encoder Instance = new();

    public byte[] EncodeCommandPacket(GprsCommandPacket command)
    {
        var imei = command.Imei ?? throw new ArgumentException("Codec 14 requires an IMEI");
        if (imei.Length > 16)
            throw new ArgumentException($"IMEI '{imei}' is longer than 16 digits");

        // IMEI is packed as 16 hex digits: a 15-digit IMEI padded with one leading zero
        var imeiBytes = Convert.FromHexString(imei.PadLeft(16, '0'));

        int dataLength = 8 + imeiBytes.Length + command.CommandText.Length; // codec + qty + type + size + imei + command + qty
        var buffer = PacketFramer.AllocatePacket(dataLength);
        var writer = new SpanWriter(buffer.AsSpan(8, dataLength));

        writer.WriteByte(0x0E);
        writer.WriteByte(0x01);
        writer.WriteByte(command.CommandType);
        writer.WriteInt32(imeiBytes.Length + command.CommandText.Length);
        writer.WriteBytes(imeiBytes);
        writer.WriteAscii(command.CommandText);
        writer.WriteByte(0x01);

        PacketFramer.SealPacket(buffer);
        return buffer;
    }
}
