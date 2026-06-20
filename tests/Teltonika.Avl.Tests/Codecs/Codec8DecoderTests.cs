using Teltonika.Avl.Models;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Codecs;

public class Codec8DecoderTests
{
    [Fact]
    public void Parse_Codec8Packet_DecodesCorrectly()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec8DataField);
        var result = AvlParser.Parse(packet);

        Assert.Equal(CodecId.Codec8, result.CodecId);
        Assert.Single(result.Records);

        var record = result.Records[0];
        Assert.Equal(Priority.Low, record.Priority);

        // GPS: 0x0F0E9D60 = 252616032 => 25.2616032; 0x209A7400 = 546993152 => 54.6993152
        Assert.Equal(25.2616032, record.Gps.Longitude, 7);
        Assert.Equal(54.6993152, record.Gps.Latitude, 7);
        Assert.Equal(0, record.Gps.Altitude);
        Assert.Equal(350, record.Gps.Angle);
        Assert.Equal(12, record.Gps.Satellites);
        Assert.Equal(0, record.Gps.Speed);

        // IO
        Assert.Equal(1, record.IoData.EventId);
        Assert.Equal(9, record.IoData.Properties.Count);

        // 1-byte IO
        Assert.Equal(1, record.IoData.Properties[0].Id);
        Assert.Equal(new byte[] { 0x01 }, record.IoData.Properties[0].Value.ToArray());

        // 2-byte IO
        Assert.Equal(2, record.IoData.Properties[1].Id);
        Assert.Equal(new byte[] { 0x00, 0x01 }, record.IoData.Properties[1].Value.ToArray());

        // 4-byte IO
        Assert.Equal(4, record.IoData.Properties[3].Id);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, record.IoData.Properties[3].Value.ToArray());

        // 8-byte IO
        Assert.Equal(7, record.IoData.Properties[6].Id);
        Assert.Equal(8, record.IoData.Properties[6].Value.Length);
    }

    [Fact]
    public void Parse_Codec8Packet_TimestampIsCorrect()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec8DataField);
        var result = AvlParser.Parse(packet);
        var record = result.Records[0];

        // 0x0000016B40D8EA30 = 1560160686000 ms => 2019-06-10T10:04:46Z
        Assert.Equal(2019, record.Timestamp.Year);
        Assert.Equal(6, record.Timestamp.Month);
        Assert.Equal(10, record.Timestamp.Day);
    }
}
