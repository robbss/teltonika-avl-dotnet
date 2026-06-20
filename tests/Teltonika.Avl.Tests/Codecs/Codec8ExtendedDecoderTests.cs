using Teltonika.Avl.Models;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Codecs;

public class Codec8ExtendedDecoderTests
{
    [Fact]
    public void Parse_Codec8ExtPacket_DecodesCorrectly()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec8ExtDataField);
        var result = AvlParser.Parse(packet);

        Assert.Equal(CodecId.Codec8Extended, result.CodecId);
        Assert.Single(result.Records);

        var record = result.Records[0];
        Assert.Equal(Priority.Low, record.Priority);

        // IO with 2-byte IDs
        Assert.Equal(1, record.IoData.EventId);
        Assert.Equal(5, record.IoData.Properties.Count);

        // 1-byte value
        Assert.Equal(1, record.IoData.Properties[0].Id);
        Assert.Equal(new byte[] { 0x01 }, record.IoData.Properties[0].Value.ToArray());

        // 2-byte value
        Assert.Equal(2, record.IoData.Properties[1].Id);
        Assert.Equal(new byte[] { 0x00, 0x05 }, record.IoData.Properties[1].Value.ToArray());

        // 4-byte value
        Assert.Equal(3, record.IoData.Properties[2].Id);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x64 }, record.IoData.Properties[2].Value.ToArray());

        // 8-byte value
        Assert.Equal(4, record.IoData.Properties[3].Id);
        Assert.Equal(8, record.IoData.Properties[3].Value.Length);

        // Variable-length value = "ABC"
        Assert.Equal(5, record.IoData.Properties[4].Id);
        Assert.Equal(new byte[] { 0x41, 0x42, 0x43 }, record.IoData.Properties[4].Value.ToArray());
    }
}
