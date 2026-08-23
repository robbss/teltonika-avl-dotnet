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

    /// <summary>Encodes an AVL data packet as a UDP datagram, as a device sends it.</summary>
    /// <param name="datagram">What to send: the packet, its identifiers and the device's IMEI.</param>
    /// <returns>The datagram, ready for a socket.</returns>
    public static byte[] EncodeUdp(UdpDatagram datagram) => UdpFramer.Write(datagram);

    /// <summary>Encodes an AVL data packet as a UDP datagram.</summary>
    /// <param name="packet">The data packet.</param>
    /// <param name="imei">The device's IMEI, which every datagram carries.</param>
    /// <param name="packetId">Datagram identifier, echoed in the acknowledgement.</param>
    /// <param name="avlPacketId">AVL packet identifier, also echoed.</param>
    /// <returns>The datagram, ready for a socket.</returns>
    public static byte[] EncodeUdp(AvlPacket packet, string imei, ushort packetId, byte avlPacketId) =>
        UdpFramer.Write(new UdpDatagram(packetId, avlPacketId, imei, packet));

    /// <summary>Encodes the acknowledgement a server owes a datagram.</summary>
    /// <param name="packetId">The datagram's packet id.</param>
    /// <param name="avlPacketId">The datagram's AVL packet id.</param>
    /// <param name="acceptedRecords">How many records were accepted.</param>
    /// <returns>The acknowledgement datagram.</returns>
    public static byte[] EncodeUdpAcknowledgement(
        ushort packetId, byte avlPacketId, byte acceptedRecords) =>
        UdpFramer.WriteAcknowledgement(packetId, avlPacketId, acceptedRecords);

    /// <summary>Encodes the acknowledgement a datagram is owed, accepting every record in it.</summary>
    /// <param name="datagram">The datagram being acknowledged.</param>
    /// <returns>The acknowledgement datagram.</returns>
    public static byte[] EncodeUdpAcknowledgement(UdpDatagram datagram) =>
        UdpFramer.WriteAcknowledgement(datagram);

    public static byte[] EncodeImei(string imei) =>
        PacketFramer.EncodeImeiFrame(imei);
}
