using Teltonika.Avl.Elements.Models;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Elements.Tests;

public class ValueEncoderTests
{
    [Fact]
    public void Encode_Boolean_WritesSingleByte()
    {
        var property = IoElementResolver.Encode(239, true);

        Assert.Equal(new byte[] { 0x01 }, property.Value.ToArray());
    }

    [Fact]
    public void Encode_Unsigned_WritesBigEndianAtDeclaredWidth()
    {
        var property = IoElementResolver.Encode(66, 12600);

        Assert.Equal(new byte[] { 0x31, 0x38 }, property.Value.ToArray());
    }

    [Fact]
    public void Encode_Unsigned_AppliesMultiplierInverse()
    {
        var property = IoElementResolver.Encode(182, 2.5);

        Assert.Equal(new byte[] { 0x00, 0x19 }, property.Value.ToArray());
    }

    [Fact]
    public void Encode_Signed_WritesTwosComplement()
    {
        var property = IoElementResolver.Encode(17, -300);

        Assert.Equal(new byte[] { 0xFE, 0xD4 }, property.Value.ToArray());
    }

    [Fact]
    public void Encode_Hex_AcceptsDelimitedString()
    {
        var property = IoElementResolver.Encode(78, "01:1A:2B:3C:4D:5E:6F:70");

        Assert.Equal(new byte[] { 0x01, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E, 0x6F, 0x70 }, property.Value.ToArray());
    }

    [Fact]
    public void Encode_Hex_LeftPadsToDeclaredWidth()
    {
        var property = IoElementResolver.Encode(78, "1A2B");

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1A, 0x2B }, property.Value.ToArray());
    }

    [Fact]
    public void Encode_VariableWidthElement_KeepsNaturalLength()
    {
        var property = IoElementResolver.Encode(385, new byte[] { 0x01, 0x02, 0x03 });

        Assert.Equal(3, property.Value.Length);
    }

    [Fact]
    public void Encode_EnumLabel_ResolvesToCode()
    {
        var property = IoElementResolver.Encode(80, "Roaming On Moving");

        Assert.Equal(new byte[] { 0x03 }, property.Value.ToArray());
    }

    [Fact]
    public void Encode_UnknownId_Throws()
    {
        Assert.Throws<ArgumentException>(() => IoElementResolver.Encode(64000, 1));
    }

    [Fact]
    public void TryEncode_UnknownId_ReturnsFalse()
    {
        Assert.False(IoElementResolver.TryEncode(64000, 1, out _));
    }

    [Theory]
    [InlineData((ushort)66, 12600.0)]
    [InlineData((ushort)182, 2.5)]
    [InlineData((ushort)16, 1234567.0)]
    [InlineData((ushort)21, 4.0)]
    public void EncodeThenResolve_RoundTripsNumericValue(ushort id, double value)
    {
        var property = IoElementResolver.Encode(id, value);
        var resolved = IoElementResolver.Resolve(property);

        Assert.NotNull(resolved);
        Assert.Equal(value, Convert.ToDouble(resolved!.Value.Value));
    }

    [Fact]
    public void EncodeThenResolve_RoundTripsForFmc234()
    {
        var property = IoElementResolver.Encode(67, 4050, TrackerModel.FMC234);
        var resolved = IoElementResolver.Resolve(property, TrackerModel.FMC234);

        Assert.NotNull(resolved);
        Assert.Equal("Battery Voltage", resolved!.Value.Name);
        Assert.Equal("mV", resolved.Value.Units);
        Assert.Equal(4050d, Convert.ToDouble(resolved.Value.Value));
    }

    [Fact]
    public void Definitions_ExposesCatalog()
    {
        Assert.True(IoElementResolver.Definitions.ContainsKey(239));
        Assert.Equal("Ignition", IoElementResolver.Definitions[239].Name);
    }

    [Fact]
    public void Encode_ProducesPropertiesCodec8CanFrame()
    {
        var record = new AvlRecord(
            DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000),
            Priority.Low,
            new GpsData(10.1569965, 56.1182665, 71, 43, 12, 90),
            new IoElement(0, [
                IoElementResolver.Encode(239, true),
                IoElementResolver.Encode(66, 12600),
                IoElementResolver.Encode(16, 1234567)
            ]));

        var bytes = AvlEncoder.Encode(new AvlPacket(CodecId.Codec8, [record]));
        var parsed = AvlParser.Parse(bytes);

        Assert.Single(parsed.Records);
        Assert.Equal(3, parsed.Records[0].IoData.Properties.Count);
    }
}
