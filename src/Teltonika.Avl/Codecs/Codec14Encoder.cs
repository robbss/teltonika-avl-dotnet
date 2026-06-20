using System.Text;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec14Encoder : ICommandCodecEncoder
{
    public static readonly Codec14Encoder Instance = new();

    public byte[] EncodeCommandPacket(GprsCommandPacket command)
    {
        var imeiBytes = Encoding.ASCII.GetBytes(command.Imei ?? throw new ArgumentException("Codec 14 requires an IMEI"));
        var commandBytes = Encoding.ASCII.GetBytes(command.CommandText);

        using var ms = new MemoryStream();
        ms.WriteByte(0x0E);
        ms.WriteByte(0x01);
        ms.WriteByte(command.CommandType);
        Codec8Encoder.WriteInt32BE(ms, imeiBytes.Length);
        ms.Write(imeiBytes);
        Codec8Encoder.WriteInt32BE(ms, commandBytes.Length);
        ms.Write(commandBytes);
        ms.WriteByte(0x01);

        return PacketFramer.Frame(ms.ToArray());
    }
}
