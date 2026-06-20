using System.Collections.Frozen;
using Teltonika.Avl.Elements.Decoding;
using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Elements.Tests;

public class ValueDecoderTests
{
    [Fact]
    public void DecodeBoolean_True()
    {
        var result = ValueDecoder.DecodeValue(new byte[] { 0x01 }, IoDataType.Boolean, 1.0, null);
        Assert.IsType<bool>(result);
        Assert.True((bool)result);
    }

    [Fact]
    public void DecodeBoolean_False()
    {
        var result = ValueDecoder.DecodeValue(new byte[] { 0x00 }, IoDataType.Boolean, 1.0, null);
        Assert.False((bool)result);
    }

    [Fact]
    public void DecodeUnsigned_1Byte()
    {
        var result = ValueDecoder.DecodeValue(new byte[] { 0x03 }, IoDataType.Unsigned, 1.0, null);
        Assert.Equal(3L, result);
    }

    [Fact]
    public void DecodeUnsigned_2Bytes_BigEndian()
    {
        var result = ValueDecoder.DecodeValue(new byte[] { 0x2E, 0xE0 }, IoDataType.Unsigned, 1.0, null);
        Assert.Equal(12000L, result);
    }

    [Fact]
    public void DecodeUnsigned_4Bytes()
    {
        var result = ValueDecoder.DecodeValue(new byte[] { 0x00, 0x01, 0x51, 0x80 }, IoDataType.Unsigned, 1.0, null);
        Assert.Equal(86400L, result);
    }

    [Fact]
    public void DecodeUnsigned_8Bytes_ReturnsUlong()
    {
        var result = ValueDecoder.DecodeValue(
            new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF },
            IoDataType.Unsigned, 1.0, null);
        Assert.IsType<ulong>(result);
        Assert.Equal(4294967295UL, (ulong)result);
    }

    [Fact]
    public void DecodeUnsigned_WithMultiplier()
    {
        // 15 * 0.1 = 1.5
        var result = ValueDecoder.DecodeValue(new byte[] { 0x00, 0x0F }, IoDataType.Unsigned, 0.1, null);
        Assert.IsType<double>(result);
        Assert.Equal(1.5, (double)result, 5);
    }

    [Fact]
    public void DecodeUnsigned_WithEnumValues()
    {
        var enums = new Dictionary<int, string>
        {
            [1] = "Harsh Acceleration",
            [2] = "Harsh Braking",
            [3] = "Harsh Cornering"
        }.ToFrozenDictionary();

        var result = ValueDecoder.DecodeValue(new byte[] { 0x02 }, IoDataType.Unsigned, 1.0, enums);
        Assert.Equal("Harsh Braking", result);
    }

    [Fact]
    public void DecodeUnsigned_WithEnumValues_UnknownValue_ReturnsLong()
    {
        var enums = new Dictionary<int, string> { [1] = "Known" }.ToFrozenDictionary();

        var result = ValueDecoder.DecodeValue(new byte[] { 0x09 }, IoDataType.Unsigned, 1.0, enums);
        Assert.Equal(9L, result);
    }

    [Fact]
    public void DecodeSigned_Negative_2Bytes()
    {
        // -100 in big-endian 2-byte signed = 0xFF9C
        var result = ValueDecoder.DecodeValue(new byte[] { 0xFF, 0x9C }, IoDataType.Signed, 1.0, null);
        Assert.Equal(-100L, result);
    }

    [Fact]
    public void DecodeSigned_WithMultiplier()
    {
        // -50 * 0.1 = -5.0
        var result = ValueDecoder.DecodeValue(new byte[] { 0xFF, 0xCE }, IoDataType.Signed, 0.1, null);
        Assert.IsType<double>(result);
        Assert.Equal(-5.0, (double)result, 5);
    }

    [Fact]
    public void DecodeHex()
    {
        var result = ValueDecoder.DecodeValue(new byte[] { 0xAA, 0xBB, 0xCC }, IoDataType.Hex, 1.0, null);
        Assert.Equal("AA:BB:CC", result);
    }

    [Fact]
    public void DecodeAscii()
    {
        var result = ValueDecoder.DecodeValue("WVWZZZ"u8.ToArray(), IoDataType.Ascii, 1.0, null);
        Assert.Equal("WVWZZZ", result);
    }
}
