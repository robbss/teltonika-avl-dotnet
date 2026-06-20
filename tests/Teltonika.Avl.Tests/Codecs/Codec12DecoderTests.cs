using Teltonika.Avl.Models;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Codecs;

public class Codec12DecoderTests
{
    [Fact]
    public void ParseCommand_Codec12Response_DecodesCorrectly()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec12ResponseDataField);
        var result = AvlParser.ParseCommand(packet);

        Assert.Equal(CodecId.Codec12, result.CodecId);
        Assert.Equal(0x06, result.CommandType);
        Assert.Equal("OK", result.CommandText);
    }

    [Fact]
    public void EncodeCommand_RoundTrips()
    {
        string command = "getinfo";
        var cmdPacket = new GprsCommandPacket(CodecId.Codec12, 0x05, command);
        var encoded = Teltonika.Avl.Codecs.Codec12Encoder.Instance.EncodeCommandPacket(cmdPacket);
        var parsed = AvlParser.ParseCommand(encoded);

        Assert.Equal(CodecId.Codec12, parsed.CodecId);
        Assert.Equal(0x05, parsed.CommandType);
        Assert.Equal(command, parsed.CommandText);
    }
}
