using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Teltonika.Avl.Codecs;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Protocol;

/// <summary>
/// The UDP channel: reads and writes the datagram envelope around an AVL data packet.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <see cref="AvlPacketReader"/> because the two envelopes have nothing in common. TCP
/// wraps a packet in a four-zero preamble, a length prefix and a trailing CRC, and learns the device's
/// IMEI once per connection from a separate handshake. UDP has no connection to hang that on, so each
/// datagram carries its own header and neither preamble nor CRC:
/// </para>
/// <code>
/// length (2)  packet id (2)  0x01 (1)  AVL packet id (1)  IMEI length (2)  IMEI (n)  AVL data field
/// </code>
/// <para>
/// The length field counts everything after itself. There is no CRC at all - UDP's own checksum is what
/// the protocol relies on, which is worth knowing before looking for one.
/// </para>
/// <para>
/// This matters beyond completeness: UDP is the sensible transport for a high-device-count fleet,
/// because it needs no socket per device and so sidesteps the ephemeral-port ceiling entirely.
/// </para>
/// </remarks>
public static class UdpFramer
{
    /// <summary>The fixed byte between the packet id and the AVL packet id. The vendor calls it unusable.</summary>
    public const byte NotUsableByte = 0x01;

    private const int LengthFieldSize = 2;
    private const int HeaderSize = 6;   // packet id (2) + unusable (1) + AVL packet id (1) + IMEI length (2)

    /// <summary>
    /// Reads a datagram.
    /// </summary>
    /// <param name="datagram">The whole datagram, starting at its length field.</param>
    /// <param name="result">The decoded datagram.</param>
    /// <param name="error">Why it could not be read, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the datagram was read.</returns>
    public static bool TryRead(ReadOnlySpan<byte> datagram, out UdpDatagram? result, out string? error)
    {
        result = null;

        if (datagram.Length < LengthFieldSize + HeaderSize)
        {
            error = "Datagram is too short to hold a UDP channel header.";
            return false;
        }

        var declared = BinaryPrimitives.ReadUInt16BigEndian(datagram);
        if (declared + LengthFieldSize != datagram.Length)
        {
            error = $"Datagram declares {declared} bytes after its length field but carries "
                + $"{datagram.Length - LengthFieldSize}.";
            return false;
        }

        var packetId = BinaryPrimitives.ReadUInt16BigEndian(datagram[2..]);
        var unusable = datagram[4];
        var avlPacketId = datagram[5];
        var imeiLength = BinaryPrimitives.ReadUInt16BigEndian(datagram[6..]);

        if (unusable != NotUsableByte)
        {
            error = $"Expected 0x{NotUsableByte:X2} after the packet id, got 0x{unusable:X2}.";
            return false;
        }

        var dataStart = LengthFieldSize + HeaderSize + imeiLength;
        if (imeiLength == 0 || dataStart >= datagram.Length)
        {
            error = $"IMEI length {imeiLength} does not fit the datagram.";
            return false;
        }

        var imei = Encoding.ASCII.GetString(datagram.Slice(LengthFieldSize + HeaderSize, imeiLength));
        var dataField = datagram[dataStart..];

        if (!CodecDecoderFactory.IsDataCodec(dataField[0]))
        {
            error = $"Codec 0x{dataField[0]:X2} is not a data codec.";
            return false;
        }

        // The decoders read from a sequence, and a datagram is one contiguous buffer - so this copy is
        // the price of sharing them, paid once per datagram rather than per record.
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(dataField.ToArray()));
        var decoder = CodecDecoderFactory.GetDataDecoder((CodecId)dataField[0]);

        if (!decoder.TryDecodeDataPacket(ref reader, out var packet, out error))
        {
            return false;
        }

        result = new UdpDatagram(packetId, avlPacketId, imei, packet!);
        error = null;
        return true;
    }

    /// <summary>Reads a datagram, throwing on anything malformed.</summary>
    /// <param name="datagram">The whole datagram, starting at its length field.</param>
    /// <returns>The decoded datagram.</returns>
    /// <exception cref="InvalidDataException">The datagram is malformed.</exception>
    public static UdpDatagram Read(ReadOnlySpan<byte> datagram) =>
        TryRead(datagram, out var result, out var error)
            ? result!
            : throw new InvalidDataException(error);

    /// <summary>
    /// Writes a datagram, as a device sends it.
    /// </summary>
    /// <param name="datagram">What to send.</param>
    /// <returns>The datagram, ready for a socket.</returns>
    /// <exception cref="ArgumentException">The IMEI is empty or the packet is not a data packet.</exception>
    public static byte[] Write(UdpDatagram datagram)
    {
        ArgumentNullException.ThrowIfNull(datagram);

        if (string.IsNullOrEmpty(datagram.Imei))
        {
            throw new ArgumentException("A UDP datagram carries its IMEI and cannot omit it.", nameof(datagram));
        }

        // The TCP encoders frame what they produce, so the data field is taken back out of the middle
        // rather than reimplementing every codec's body for UDP.
        var framed = CodecEncoderFactory.GetDataEncoder(datagram.Packet.CodecId).EncodeDataPacket(datagram.Packet);
        var dataField = framed.AsSpan(8, framed.Length - 12);

        var imeiLength = Encoding.ASCII.GetByteCount(datagram.Imei);
        var afterLength = HeaderSize + imeiLength + dataField.Length;
        var buffer = new byte[LengthFieldSize + afterLength];
        var writer = new SpanWriter(buffer);

        writer.WriteUInt16((ushort)afterLength);
        writer.WriteUInt16(datagram.PacketId);
        writer.WriteByte(NotUsableByte);
        writer.WriteByte(datagram.AvlPacketId);
        writer.WriteUInt16((ushort)imeiLength);
        writer.WriteAscii(datagram.Imei);
        writer.WriteBytes(dataField);

        return buffer;
    }

    /// <summary>
    /// Writes the acknowledgement a server owes a datagram.
    /// </summary>
    /// <remarks>
    /// Both identifiers are echoed so the device can match the acknowledgement to what it sent, and the
    /// accepted count plays the part the record count plays over TCP.
    /// </remarks>
    /// <param name="packetId">The datagram's packet id.</param>
    /// <param name="avlPacketId">The datagram's AVL packet id.</param>
    /// <param name="acceptedRecords">How many records were accepted.</param>
    /// <returns>The acknowledgement datagram.</returns>
    public static byte[] WriteAcknowledgement(ushort packetId, byte avlPacketId, byte acceptedRecords)
    {
        var buffer = new byte[7];
        var writer = new SpanWriter(buffer);

        writer.WriteUInt16(5);   // packet id (2) + unusable (1) + AVL packet id (1) + accepted (1)
        writer.WriteUInt16(packetId);
        writer.WriteByte(NotUsableByte);
        writer.WriteByte(avlPacketId);
        writer.WriteByte(acceptedRecords);

        return buffer;
    }

    /// <summary>Writes the acknowledgement a datagram is owed, accepting every record in it.</summary>
    /// <param name="datagram">The datagram being acknowledged.</param>
    /// <returns>The acknowledgement datagram.</returns>
    public static byte[] WriteAcknowledgement(UdpDatagram datagram)
    {
        ArgumentNullException.ThrowIfNull(datagram);

        return WriteAcknowledgement(
            datagram.PacketId, datagram.AvlPacketId, (byte)datagram.Packet.Records.Count);
    }

    /// <summary>
    /// Reads an acknowledgement, as a device does.
    /// </summary>
    /// <param name="datagram">The acknowledgement datagram.</param>
    /// <param name="packetId">The echoed packet id.</param>
    /// <param name="avlPacketId">The echoed AVL packet id.</param>
    /// <param name="acceptedRecords">How many records the server accepted.</param>
    /// <returns><see langword="true"/> when the acknowledgement was read.</returns>
    public static bool TryReadAcknowledgement(
        ReadOnlySpan<byte> datagram, out ushort packetId, out byte avlPacketId, out byte acceptedRecords)
    {
        packetId = 0;
        avlPacketId = 0;
        acceptedRecords = 0;

        if (datagram.Length < 7 || BinaryPrimitives.ReadUInt16BigEndian(datagram) != 5)
        {
            return false;
        }

        packetId = BinaryPrimitives.ReadUInt16BigEndian(datagram[2..]);
        if (datagram[4] != NotUsableByte)
        {
            return false;
        }

        avlPacketId = datagram[5];
        acceptedRecords = datagram[6];
        return true;
    }
}
