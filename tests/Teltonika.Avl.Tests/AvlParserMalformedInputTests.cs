using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests;

public class AvlParserMalformedInputTests
{
    // Codec 8 field claiming 2 records but containing only 1 (30 bytes: header + gps + empty IO)
    private const string TruncatedCodec8Field =
        "08" + "02" +
        "0000016B40D8EA30" + "00" + "0F0E9D60209A74000000015E0C0000" + "01" + "00" + "00000000" +
        "02";

    // Codec 8 field whose trailing record count does not match the leading one
    private const string CountMismatchCodec8Field =
        "08" + "01" +
        "0000016B40D8EA30" + "00" + "0F0E9D60209A74000000015E0C0000" + "01" + "00" + "00000000" +
        "02";

    // Codec 12 command whose declared size is far larger than the remaining payload
    private const string OversizedCodec12Field =
        "0C" + "01" + "05" + "7FFFFFFF" + "01";

    [Fact]
    public void TryParse_TruncatedDataField_ReturnsFalse()
    {
        var packet = SamplePackets.BuildPacket(TruncatedCodec8Field);
        Assert.False(AvlParser.TryParse(packet, out var parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void Parse_TruncatedDataField_ThrowsInvalidData()
    {
        var packet = SamplePackets.BuildPacket(TruncatedCodec8Field);
        var ex = Assert.Throws<InvalidDataException>(() => AvlParser.Parse(packet));
        Assert.Contains("end of data field", ex.Message);
    }

    [Fact]
    public void TryParse_RecordCountMismatch_ReturnsFalse()
    {
        var packet = SamplePackets.BuildPacket(CountMismatchCodec8Field);
        Assert.False(AvlParser.TryParse(packet, out _));
    }

    [Fact]
    public void Parse_RecordCountMismatch_ThrowsInvalidData()
    {
        var packet = SamplePackets.BuildPacket(CountMismatchCodec8Field);
        var ex = Assert.Throws<InvalidDataException>(() => AvlParser.Parse(packet));
        Assert.Contains("Record count mismatch", ex.Message);
    }

    [Fact]
    public void TryParse_CorruptCrc_ReturnsFalse()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec8DataField);
        packet[^1] ^= 0xFF;
        Assert.False(AvlParser.TryParse(packet, out _));
    }

    [Fact]
    public void TryParse_IncompleteBuffer_ReturnsFalse()
    {
        Assert.False(AvlParser.TryParse(new byte[] { 0x00, 0x00, 0x00 }, out _));
    }

    [Fact]
    public void TryParse_CommandPacket_ReturnsFalse()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec12RequestDataField);
        Assert.False(AvlParser.TryParse(packet, out _));
    }

    [Fact]
    public void TryParseCommand_OversizedCommandLength_ReturnsFalse()
    {
        var packet = SamplePackets.BuildPacket(OversizedCodec12Field);
        Assert.False(AvlParser.TryParseCommand(packet, out var parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void ParseCommand_OversizedCommandLength_ThrowsInvalidData()
    {
        var packet = SamplePackets.BuildPacket(OversizedCodec12Field);
        var ex = Assert.Throws<InvalidDataException>(() => AvlParser.ParseCommand(packet));
        Assert.Contains("Invalid command size", ex.Message);
    }

    [Fact]
    public void TryParseCommand_ValidPacket_ReturnsTrue()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec12RequestDataField);
        Assert.True(AvlParser.TryParseCommand(packet, out var parsed));
        Assert.Equal("getinfo", parsed!.CommandText);
    }

    [Fact]
    public void TryParseImei_ValidAndTruncatedFrames()
    {
        Assert.True(AvlParser.TryParseImei(SamplePackets.HexToBytes(SamplePackets.ImeiHex), out var imei));
        Assert.Equal("356307042441013", imei);

        Assert.False(AvlParser.TryParseImei(SamplePackets.HexToBytes("000F3335"), out imei));
        Assert.Null(imei);
    }
}
