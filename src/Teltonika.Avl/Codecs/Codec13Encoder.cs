using System.Text;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec13Encoder : ICommandCodecEncoder
{
    public static readonly Codec13Encoder Instance = new();

    public byte[] EncodeCommandPacket(GprsCommandPacket command)
    {
        var commandBytes = Encoding.ASCII.GetBytes(command.CommandText);
        int timestampSeconds = command.Timestamp.HasValue
            ? (int)command.Timestamp.Value.ToUnixTimeSeconds()
            : (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using var ms = new MemoryStream();
        ms.WriteByte(0x0D);
        ms.WriteByte(0x01);
        ms.WriteByte(command.CommandType);
        Codec8Encoder.WriteInt32BE(ms, timestampSeconds);
        Codec8Encoder.WriteInt32BE(ms, commandBytes.Length);
        ms.Write(commandBytes);
        ms.WriteByte(0x01);

        return PacketFramer.Frame(ms.ToArray());
    }
}
