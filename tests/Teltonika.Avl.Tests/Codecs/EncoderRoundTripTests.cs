using Teltonika.Avl.Models;

namespace Teltonika.Avl.Tests.Codecs;

public class EncoderRoundTripTests
{
    private static AvlRecord MakeRecord()
    {
        var gps = new GpsData(25.2616032, 54.6993152, 100, 350, 12, 60);
        var io = new IoElement(1, new IoProperty[]
        {
            new(1, new byte[] { 0x01 }),
            new(2, new byte[] { 0x00, 0x05 }),
            new(3, new byte[] { 0x00, 0x00, 0x00, 0x64 }),
            new(4, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0xE8 }),
        });
        return new AvlRecord(
            DateTimeOffset.Parse("2024-01-15T12:00:00Z"),
            Priority.High,
            gps,
            io);
    }

    [Fact]
    public void Codec8_RoundTrip()
    {
        var original = new AvlPacket(CodecId.Codec8, [MakeRecord()]);
        var encoded = AvlEncoder.Encode(original);
        var decoded = AvlParser.Parse(encoded);

        AssertPacketEqual(original, decoded);
    }

    [Fact]
    public void Codec8Extended_RoundTrip()
    {
        var record = MakeRecord();
        // Add a variable-length IO property
        var props = record.IoData.Properties.ToList();
        props.Add(new IoProperty(5, new byte[] { 0x41, 0x42, 0x43 })); // "ABC"
        var recordWithVar = record with { IoData = new IoElement(record.IoData.EventId, props) };

        var original = new AvlPacket(CodecId.Codec8Extended, [recordWithVar]);
        var encoded = AvlEncoder.Encode(original);
        var decoded = AvlParser.Parse(encoded);

        AssertPacketEqual(original, decoded);
    }

    [Fact]
    public void Codec16_RoundTrip()
    {
        var original = new AvlPacket(CodecId.Codec16, [MakeRecord()]);
        var encoded = AvlEncoder.Encode(original);
        var decoded = AvlParser.Parse(encoded);

        AssertPacketEqual(original, decoded);
    }

    [Fact]
    public void Codec8_MultipleRecords_RoundTrip()
    {
        var record1 = MakeRecord();
        var record2 = MakeRecord() with
        {
            Timestamp = DateTimeOffset.Parse("2024-01-15T12:01:00Z"),
            Priority = Priority.Panic,
            Gps = new GpsData(25.2620, 54.7000, 105, 90, 10, 80)
        };

        var original = new AvlPacket(CodecId.Codec8, [record1, record2]);
        var encoded = AvlEncoder.Encode(original);
        var decoded = AvlParser.Parse(encoded);

        Assert.Equal(2, decoded.Records.Count);
        AssertPacketEqual(original, decoded);
    }

    [Fact]
    public void Codec12_Command_RoundTrip()
    {
        var original = new GprsCommandPacket(CodecId.Codec12, 0x05, "getinfo");
        var encoded = AvlEncoder.EncodeCommand(original);
        var decoded = AvlParser.ParseCommand(encoded);

        Assert.Equal(original.CodecId, decoded.CodecId);
        Assert.Equal(original.CommandType, decoded.CommandType);
        Assert.Equal(original.CommandText, decoded.CommandText);
    }

    [Fact]
    public void Codec13_Command_RoundTrip()
    {
        var ts = DateTimeOffset.Parse("2024-01-15T12:00:00Z");
        var original = new GprsCommandPacket(CodecId.Codec13, 0x06, "status OK", Timestamp: ts);
        var encoded = AvlEncoder.EncodeCommand(original);
        var decoded = AvlParser.ParseCommand(encoded);

        Assert.Equal(original.CodecId, decoded.CodecId);
        Assert.Equal(original.CommandType, decoded.CommandType);
        Assert.Equal(original.CommandText, decoded.CommandText);
        Assert.NotNull(decoded.Timestamp);
        Assert.Equal(ts.ToUnixTimeSeconds(), decoded.Timestamp!.Value.ToUnixTimeSeconds());
    }

    [Fact]
    public void Codec14_Command_RoundTrip()
    {
        var original = new GprsCommandPacket(CodecId.Codec14, 0x05, "getver", Imei: "356307042441013");
        var encoded = AvlEncoder.EncodeCommand(original);
        var decoded = AvlParser.ParseCommand(encoded);

        Assert.Equal(original.CodecId, decoded.CodecId);
        Assert.Equal(original.CommandType, decoded.CommandType);
        Assert.Equal(original.CommandText, decoded.CommandText);
        Assert.Equal(original.Imei, decoded.Imei);
    }

    [Fact]
    public void ImeiFrame_RoundTrip()
    {
        string imei = "356307042441013";
        var encoded = AvlEncoder.EncodeImei(imei);
        var decoded = AvlParser.ParseImei(encoded);
        Assert.Equal(imei, decoded);
    }

    private static void AssertPacketEqual(AvlPacket expected, AvlPacket actual)
    {
        Assert.Equal(expected.Records.Count, actual.Records.Count);

        for (int i = 0; i < expected.Records.Count; i++)
        {
            var e = expected.Records[i];
            var a = actual.Records[i];

            Assert.Equal(e.Timestamp.ToUnixTimeMilliseconds(), a.Timestamp.ToUnixTimeMilliseconds());
            Assert.Equal(e.Priority, a.Priority);

            // GPS — compare with tolerance for double→int→double conversion
            Assert.Equal(e.Gps.Longitude, a.Gps.Longitude, 6);
            Assert.Equal(e.Gps.Latitude, a.Gps.Latitude, 6);
            Assert.Equal(e.Gps.Altitude, a.Gps.Altitude);
            Assert.Equal(e.Gps.Angle, a.Gps.Angle);
            Assert.Equal(e.Gps.Satellites, a.Gps.Satellites);
            Assert.Equal(e.Gps.Speed, a.Gps.Speed);

            // IO
            Assert.Equal(e.IoData.Properties.Count, a.IoData.Properties.Count);
            for (int j = 0; j < e.IoData.Properties.Count; j++)
            {
                Assert.Equal(e.IoData.Properties[j].Id, a.IoData.Properties[j].Id);
                Assert.Equal(
                    e.IoData.Properties[j].Value.ToArray(),
                    a.IoData.Properties[j].Value.ToArray());
            }
        }
    }
}
