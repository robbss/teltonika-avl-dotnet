using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Protocol;

/// <summary>
/// The UDP channel, against both datagrams Teltonika publishes for it.
/// </summary>
/// <remarks>
/// <para>
/// The UDP form is not the TCP form in a datagram, which is why none of the TCP reader's tests cover it:
/// there is no preamble, no CRC, and the IMEI travels in every datagram instead of arriving once from a
/// handshake. A parser built for one cannot accept the other.
/// </para>
/// <para>
/// Both vectors are self-verifying through their length field, which counts everything after itself -
/// 61 bytes for the Codec 8 one and 95 for the Codec 8 Extended one.
/// </para>
/// </remarks>
public class UdpFramerTests
{
    // https://wiki.teltonika-gps.com/view/Codec — "Codec 8 UDP" example, 63 bytes.
    private const string Codec8Datagram =
        "003DCAFE0105000F33353230393330383634303336353508010000016B4F815B30" +
        "010000000000000000000000000000000103021503010101425DBC000001";

    // The same page's Codec 8 Extended UDP example, 97 bytes.
    private const string Codec8ExtendedDatagram =
        "005FCAFE0107000F3335323039333038363430333635358E010000016B4F831C68" +
        "0100000000000000000000000000000000010005000100010100010011009D0001" +
        "0010015E2C880002000B000000003544C87A000E000000001DD7E06A000001";

    // The acknowledgement the same example shows the server replying with.
    private const string Acknowledgement = "0005CAFE010501";

    private const string Imei = "352093086403655";

    [Fact]
    public void TheCodec8Datagram_Reads()
    {
        var raw = SamplePackets.HexToBytes(Codec8Datagram);

        Assert.True(UdpFramer.TryRead(raw, out var datagram, out var error), error);
        Assert.NotNull(datagram);

        Assert.Equal(0xCAFE, datagram.PacketId);
        Assert.Equal(0x05, datagram.AvlPacketId);
        Assert.Equal(Imei, datagram.Imei);
        Assert.Equal(CodecId.Codec8, datagram.Packet.CodecId);
        Assert.Single(datagram.Packet.Records);
    }

    [Fact]
    public void TheCodec8ExtendedDatagram_Reads()
    {
        var datagram = UdpFramer.Read(SamplePackets.HexToBytes(Codec8ExtendedDatagram));

        Assert.Equal(0xCAFE, datagram.PacketId);
        Assert.Equal(0x07, datagram.AvlPacketId);
        Assert.Equal(Imei, datagram.Imei);
        Assert.Equal(CodecId.Codec8Extended, datagram.Packet.CodecId);
        Assert.Single(datagram.Packet.Records);

        // The 8E record carries the four-, eight- and variable-width elements the example lists.
        var io = datagram.Packet.Records[0].IoData;
        Assert.Equal(1, io.EventId);
        Assert.Equal(5, io.Properties.Count);
    }

    [Theory]
    [InlineData(Codec8Datagram)]
    [InlineData(Codec8ExtendedDatagram)]
    public void AVendorDatagram_ReEncodesByteForByte(string hex)
    {
        var raw = SamplePackets.HexToBytes(hex);

        var reEncoded = UdpFramer.Write(UdpFramer.Read(raw));

        Assert.Equal(raw, reEncoded);
    }

    [Fact]
    public void TheAcknowledgement_MatchesTheDocumentedBytes()
    {
        var expected = SamplePackets.HexToBytes(Acknowledgement);

        var written = UdpFramer.WriteAcknowledgement(packetId: 0xCAFE, avlPacketId: 0x05, acceptedRecords: 1);

        Assert.Equal(expected, written);
    }

    [Fact]
    public void TheAcknowledgement_CanBeBuiltFromTheDatagramItAnswers()
    {
        var datagram = UdpFramer.Read(SamplePackets.HexToBytes(Codec8Datagram));

        // One record in the datagram, so one accepted - and both identifiers echoed.
        Assert.Equal(
            SamplePackets.HexToBytes(Acknowledgement),
            UdpFramer.WriteAcknowledgement(datagram));
    }

    [Fact]
    public void TheAcknowledgement_ReadsBack()
    {
        var raw = SamplePackets.HexToBytes(Acknowledgement);

        Assert.True(UdpFramer.TryReadAcknowledgement(raw, out var packetId, out var avlPacketId, out var accepted));

        Assert.Equal(0xCAFE, packetId);
        Assert.Equal(0x05, avlPacketId);
        Assert.Equal(1, accepted);
    }

    [Fact]
    public void ADatagramRoundTripsThroughTheFacade()
    {
        var packet = AvlParser.Parse(SamplePackets.BuildPacket(SamplePackets.Codec8DataField));

        var encoded = AvlEncoder.EncodeUdp(packet, "350000000000001", packetId: 0x1234, avlPacketId: 0x09);

        Assert.True(AvlParser.TryParseUdp(encoded, out var decoded));
        Assert.NotNull(decoded);
        Assert.Equal(0x1234, decoded.PacketId);
        Assert.Equal(0x09, decoded.AvlPacketId);
        Assert.Equal("350000000000001", decoded.Imei);
        Assert.Equal(packet.Records.Count, decoded.Packet.Records.Count);
    }

    // A datagram has no CRC, so the length field is the only integrity check there is - which makes
    // enforcing it the difference between rejecting a truncated datagram and decoding garbage.
    [Fact]
    public void ATruncatedDatagram_IsRejected()
    {
        var raw = SamplePackets.HexToBytes(Codec8Datagram);

        Assert.False(UdpFramer.TryRead(raw.AsSpan(0, raw.Length - 4), out _, out var error));
        Assert.Contains("declares", error!, StringComparison.Ordinal);
    }

    [Fact]
    public void ADatagramWithATrailingByte_IsRejected()
    {
        var raw = SamplePackets.HexToBytes(Codec8Datagram);
        var padded = new byte[raw.Length + 1];
        raw.CopyTo(padded, 0);

        Assert.False(UdpFramer.TryRead(padded, out _, out _));
    }

    [Fact]
    public void ADatagramTooShortForItsHeader_IsRejected()
    {
        Assert.False(UdpFramer.TryRead([0x00, 0x05, 0xCA], out _, out var error));
        Assert.Contains("too short", error!, StringComparison.Ordinal);
    }

    [Fact]
    public void ADatagramWithoutTheFixedByte_IsRejected()
    {
        var raw = SamplePackets.HexToBytes(Codec8Datagram);
        raw[4] = 0x02;

        Assert.False(UdpFramer.TryRead(raw, out _, out var error));
        Assert.Contains("0x01", error!, StringComparison.Ordinal);
    }

    [Fact]
    public void ADatagramWithAnImpossibleImeiLength_IsRejected()
    {
        var raw = SamplePackets.HexToBytes(Codec8Datagram);
        raw[6] = 0xFF;
        raw[7] = 0xFF;

        Assert.False(UdpFramer.TryRead(raw, out _, out var error));
        Assert.Contains("IMEI length", error!, StringComparison.Ordinal);
    }

    [Fact]
    public void ADatagramCarryingACommandCodec_IsRejected()
    {
        // Codec 12 is a command codec; the UDP data channel does not carry one.
        var raw = SamplePackets.HexToBytes(Codec8Datagram);
        raw[23] = 0x0C;

        Assert.False(UdpFramer.TryRead(raw, out _, out var error));
        Assert.Contains("not a data codec", error!, StringComparison.Ordinal);
    }

    [Fact]
    public void WritingWithoutAnImei_IsRefused()
    {
        var packet = AvlParser.Parse(SamplePackets.BuildPacket(SamplePackets.Codec8DataField));

        Assert.Throws<ArgumentException>(() =>
            UdpFramer.Write(new UdpDatagram(1, 1, string.Empty, packet)));
    }
}
