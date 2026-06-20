using Teltonika.Avl.Codecs;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl;

public static class AvlEncoder
{
    public static byte[] Encode(AvlPacket packet)
    {
        var encoder = CodecEncoderFactory.GetDataEncoder(packet.CodecId);
        return encoder.EncodeDataPacket(packet);
    }

    public static byte[] EncodeCommand(GprsCommandPacket command)
    {
        var encoder = CodecEncoderFactory.GetCommandEncoder(command.CodecId);
        return encoder.EncodeCommandPacket(command);
    }

    public static byte[] EncodeImei(string imei) =>
        PacketFramer.EncodeImeiFrame(imei);
}
