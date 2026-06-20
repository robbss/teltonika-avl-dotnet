using System.Text;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec12Encoder : ICommandCodecEncoder
{
    public static readonly Codec12Encoder Instance = new();

    public byte[] EncodeCommandPacket(GprsCommandPacket command)
    {
        var commandBytes = Encoding.ASCII.GetBytes(command.CommandText);

        using var ms = new MemoryStream();
        ms.WriteByte(0x0C);
        ms.WriteByte(0x01);
        ms.WriteByte(command.CommandType);
        Codec8Encoder.WriteInt32BE(ms, commandBytes.Length);
        ms.Write(commandBytes);
        ms.WriteByte(0x01);

        return PacketFramer.Frame(ms.ToArray());
    }
}
