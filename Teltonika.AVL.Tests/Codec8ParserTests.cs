using Teltonika.AVL.Protocol;

namespace Teltonika.AVL.Tests;

public class Codec8ParserTests
{
    [Fact]
    public void Parse_InvalidLength_ReturnsFalse()
    {
        var parser = new Codec8Parser();
        byte[] data = new byte[] { 0x08 }; // Too short

        bool result = parser.TryParse(data, out var packet);

        Assert.False(result);
        Assert.Null(packet);
    }

    [Fact]
    public void Parse_InvalidCodecId_ReturnsFalse()
    {
        var parser = new Codec8Parser();
        byte[] data = new byte[] { 0x09, 0x01, 0x00, 0x00 }; // Wrong codec

        bool result = parser.TryParse(data, out var packet);

        Assert.False(result);
        Assert.Null(packet);
    }
}