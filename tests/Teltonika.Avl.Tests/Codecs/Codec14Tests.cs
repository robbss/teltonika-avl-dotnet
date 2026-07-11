using Teltonika.Avl.Models;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Codecs;

public class Codec14Tests
{
    // Official example from wiki.teltonika-gps.com/view/Codec (Codec 14):
    // "getver" command for IMEI 352093081452251, including CRC.
    private const string OfficialCommandPacketHex =
        "00000000000000160E01050000000E0352093081452251676574766572010000D2C1";

    [Fact]
    public void ParseCommand_OfficialCodec14Packet_DecodesCorrectly()
    {
        var packet = SamplePackets.HexToBytes(OfficialCommandPacketHex);
        var result = AvlParser.ParseCommand(packet);

        Assert.Equal(CodecId.Codec14, result.CodecId);
        Assert.Equal(0x05, result.CommandType);
        Assert.Equal("352093081452251", result.Imei);
        Assert.Equal("getver", result.CommandText);
    }

    [Fact]
    public void EncodeCommand_MatchesOfficialPacketBytes()
    {
        var cmdPacket = new GprsCommandPacket(CodecId.Codec14, 0x05, "getver", Imei: "352093081452251");
        var encoded = Teltonika.Avl.Codecs.Codec14Encoder.Instance.EncodeCommandPacket(cmdPacket);

        Assert.Equal(SamplePackets.HexToBytes(OfficialCommandPacketHex), encoded);
    }

    [Fact]
    public void EncodeCommand_RoundTrips()
    {
        var cmdPacket = new GprsCommandPacket(CodecId.Codec14, 0x05, "getinfo", Imei: "356307042441013");
        var encoded = Teltonika.Avl.Codecs.Codec14Encoder.Instance.EncodeCommandPacket(cmdPacket);
        var parsed = AvlParser.ParseCommand(encoded);

        Assert.Equal(CodecId.Codec14, parsed.CodecId);
        Assert.Equal(0x05, parsed.CommandType);
        Assert.Equal("356307042441013", parsed.Imei);
        Assert.Equal("getinfo", parsed.CommandText);
    }

    [Fact]
    public void EncodeCommand_WithoutImei_Throws()
    {
        var cmdPacket = new GprsCommandPacket(CodecId.Codec14, 0x05, "getver");
        Assert.Throws<ArgumentException>(
            () => Teltonika.Avl.Codecs.Codec14Encoder.Instance.EncodeCommandPacket(cmdPacket));
    }
}
