using Teltonika.Avl.Models;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Codecs;

/// <summary>
/// Codec 16, against the packet Teltonika publishes for it.
/// </summary>
/// <remarks>
/// <para>
/// This vector is why the decoder was wrong. Codec 16 carries a generation type byte per <em>record</em>,
/// between the event id and the total count - and both the decoder and the encoder assumed one per
/// value-size group as well. Being wrong in the same way on both sides is what made it survive: a
/// round-trip test agreed with itself perfectly, and the vendor's own bytes were the only thing that
/// could tell.
/// </para>
/// <para>
/// The packet is self-verifying: its declared data length is 95, its CRC-16/IBM is 0x5FB3, and its
/// trailing record count must match the leading one. Any of those failing would mean the transcription
/// is wrong rather than the library.
/// </para>
/// </remarks>
public class Codec16Tests
{
    // https://wiki.teltonika-gps.com/view/Codec — "Codec 16" example, 107 bytes, two records.
    private const string VendorPacket =
        "000000000000005F10020000016BDBC783300000000000000000000000000000000000" +
        "0B05040200010000030002000B00270042563A00000000016BDBC7871800000000000" +
        "000000000000000000000000B05040200010000030002000B00260042563A00000200005FB3";

    [Fact]
    public void TheVendorPacket_Decodes()
    {
        var raw = SamplePackets.HexToBytes(VendorPacket);

        Assert.True(AvlParser.TryParse(raw, out var packet), "the published Codec 16 packet did not decode.");
        Assert.NotNull(packet);
        Assert.Equal(CodecId.Codec16, packet.CodecId);
        Assert.Equal(2, packet.Records.Count);
    }

    [Fact]
    public void TheVendorPacket_DecodesToTheDocumentedValues()
    {
        var packet = AvlParser.Parse(SamplePackets.HexToBytes(VendorPacket));

        var first = packet.Records[0];
        Assert.Equal(Priority.Low, first.Priority);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(0x0000016BDBC78330), first.Timestamp);

        // Event id 11 - GSM signal - and the generation type the packet actually carries.
        Assert.Equal(11, first.IoData.EventId);
        Assert.Equal(0x05, first.IoData.GenerationType);

        // Four properties: two one-byte, two two-byte, matching the declared total of 4.
        Assert.Equal(4, first.IoData.Properties.Count);
        Assert.Equal([1, 3, 11, 66], first.IoData.Properties.Select(p => (int)p.Id));
        Assert.Equal([1, 1, 2, 2], first.IoData.Properties.Select(p => p.Value.Length));

        Assert.Equal([0x00, 0x27], first.IoData.Properties[2].Value.ToArray());   // GSM signal
        Assert.Equal([0x56, 0x3A], first.IoData.Properties[3].Value.ToArray());   // external voltage

        // The second record differs only in its timestamp and one value, which is what makes it a
        // useful second record rather than a copy.
        var second = packet.Records[1];
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(0x0000016BDBC78718), second.Timestamp);
        Assert.Equal([0x00, 0x26], second.IoData.Properties[2].Value.ToArray());
    }

    // The strongest statement available without hardware: the vendor's bytes come back out unchanged.
    [Fact]
    public void TheVendorPacket_ReEncodesByteForByte()
    {
        var raw = SamplePackets.HexToBytes(VendorPacket);

        var reEncoded = AvlEncoder.Encode(AvlParser.Parse(raw));

        Assert.Equal(raw, reEncoded);
    }

    [Fact]
    public void AGenerationType_SurvivesARoundTrip()
    {
        // Without this on the model, a decoded packet could not be re-encoded as itself.
        var packet = new AvlPacket(CodecId.Codec16, [
            new AvlRecord(
                DateTimeOffset.FromUnixTimeMilliseconds(1_500_000_000_000),
                Priority.High,
                new GpsData(25.3032016, 54.7146368, 100, 90, 10, 60),
                new IoElement(155, [new IoProperty(1, new byte[] { 0x01 })], GenerationType: 0x03)),
        ]);

        var decoded = AvlParser.Parse(AvlEncoder.Encode(packet));

        Assert.Equal(0x03, decoded.Records[0].IoData.GenerationType);
        Assert.Equal(155, decoded.Records[0].IoData.EventId);
    }

    [Fact]
    public void OtherCodecs_LeaveTheGenerationTypeAtZero()
    {
        // Codec 8 has no such field, so a decoded Codec 8 record must not invent one.
        var packet = AvlParser.Parse(SamplePackets.BuildPacket(SamplePackets.Codec8DataField));

        Assert.Equal(0, packet.Records[0].IoData.GenerationType);
    }

    [Fact]
    public void AllFourValueWidths_RoundTrip()
    {
        var packet = new AvlPacket(CodecId.Codec16, [
            new AvlRecord(
                DateTimeOffset.FromUnixTimeMilliseconds(1_500_000_000_000),
                Priority.Panic,
                new GpsData(25.3, 54.7, 100, 90, 10, 60),
                new IoElement(
                    1,
                    [
                        new IoProperty(21, new byte[] { 0x04 }),
                        new IoProperty(66, new byte[] { 0x56, 0x3A }),
                        new IoProperty(16, new byte[] { 0x00, 0x01, 0xE2, 0x40 }),
                        new IoProperty(241, new byte[] { 0, 0, 0, 0, 0, 0, 0x60, 0x1A }),
                    ],
                    GenerationType: 0x01)),
        ]);

        var decoded = AvlParser.Parse(AvlEncoder.Encode(packet));
        var io = decoded.Records[0].IoData;

        Assert.Equal(4, io.Properties.Count);
        Assert.Equal([1, 2, 4, 8], io.Properties.Select(p => p.Value.Length));
        Assert.Equal([21, 66, 16, 241], io.Properties.Select(p => (int)p.Id));
    }

    [Fact]
    public void ATruncatedPacket_IsRejectedRatherThanGuessed()
    {
        var raw = SamplePackets.HexToBytes(VendorPacket);

        // Chop the last record's tail off, keeping the envelope intact enough to reach the decoder.
        Assert.False(AvlParser.TryParse(raw.AsSpan(0, 60).ToArray(), out _));
    }
}
