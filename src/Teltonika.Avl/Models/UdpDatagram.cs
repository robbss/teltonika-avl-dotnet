namespace Teltonika.Avl.Models;

/// <summary>
/// One AVL data packet as it arrives over UDP.
/// </summary>
/// <remarks>
/// <para>
/// The UDP form is not the TCP form in a datagram. TCP frames a packet with a four-zero preamble, a
/// length prefix and a trailing CRC, and identifies the device once per connection with a separate IMEI
/// handshake. UDP has no connection to hang that on, so every datagram carries its own header - length,
/// packet id, AVL packet id and the IMEI inline - and drops the preamble and the CRC entirely.
/// </para>
/// <para>
/// That is why the two cannot share a reader, and why <see cref="Protocol.UdpFramer"/> exists rather
/// than a flag on the TCP one.
/// </para>
/// </remarks>
/// <param name="PacketId">
/// Datagram identifier, echoed in the acknowledgement so the device can match it. The vendor's examples
/// use <c>0xCAFE</c>.
/// </param>
/// <param name="AvlPacketId">
/// Identifier of the AVL packet inside, also echoed. Lets a device resend without the server
/// double-counting.
/// </param>
/// <param name="Imei">The device's IMEI, carried in every datagram rather than once per connection.</param>
/// <param name="Packet">The AVL data packet itself.</param>
public sealed record UdpDatagram(
    ushort PacketId,
    byte AvlPacketId,
    string Imei,
    AvlPacket Packet);
