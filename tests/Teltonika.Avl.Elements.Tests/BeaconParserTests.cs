using Teltonika.Avl.Elements.Beacons;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Elements.Tests;

public class BeaconParserTests
{
    private static byte[] HexToBytes(string hex)
    {
        hex = hex.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    // Official example from wiki.teltonika-gps.com "How to start with FMB devices and Beacons?":
    // AVL ID 385 value with 8 beacons (5 iBeacon, 3 Eddystone; one Eddystone with battery + temperature).
    private const string BeaconListHex =
        "11" +
        "21" + "4B5C049F515341FCA950D2C264414E10" + "0005" + "0006" + "BA" +
        "21" + "E2C56DB5DFFB48D2B060D0F5A71096E0" + "0000" + "0000" + "A9" +
        "21" + "31A74BB76A79423196C916CFB9FAED45" + "002D" + "0015" + "9F" +
        "07" + "00112233445566778899" + "ABCDE0810047" + "AE" + "0BE8" + "0015" +
        "21" + "0F86676BEC91420A94409110029AFAC4" + "15B3" + "1A0A" + "A1" +
        "01" + "DE9C18E92CA5AA689697" + "365434663222" + "BA" +
        "21" + "EBBBDE835D7F4965B5F06C2EDCB3A553" + "0001" + "0080" + "A5" +
        "01" + "736B79686F73742E646B" + "000010000128" + "AD";

    // Official example from wiki.teltonika-gps.com "FMB003 Beacon List":
    // AVL ID 548 value with 4 beacons, each RSSI + 16-byte beacon ID + 31 bytes additional data.
    private const string AdvancedBeaconHex =
        "01" +
        "360001B10110F34B6F6AA38255AA9EF619154E2D0055021F0201060303AAFE1716AAFE0002F34B6F6AA38255AA9EF619154E2D00550000" +
        "360001AB0110E987706AA38255AA94321B154E2D0055021F0201060303AAFE1716AAFE0002E987706AA38255AA94321B154E2D00550000" +
        "360001A801101E74706AA38255FAABCD000000000000021F0201060303AAFE1716AAFE00021E74706AA38255FAABCD0000000000000000" +
        "360001A201100C8C6F6BA38255AAB7361A164E2D0055021F0201060303AAFE1716AAFE00020C8C6F6BA38255AAB7361A164E2D00550000";

    // Full Codec 8 Extended packet carrying the beacon list above as IO property 385 (same wiki page).
    private const string BeaconPacketHex =
        "00000000000000D68E01000001701F9B3FA9000F0E5732209AB450006800290400000181000100000000000000000001018100A9" +
        "11214B5C049F515341FCA950D2C264414E1000050006BA21E2C56DB5DFFB48D2B060D0F5A71096E000000000A92131A74BB76A79" +
        "423196C916CFB9FAED45002D00159F0700112233445566778899ABCDE0810047AE0BE80015210F86676BEC91420A94409110029A" +
        "FAC415B31A0AA101DE9C18E92CA5AA689697365434663222BA21EBBBDE835D7F4965B5F06C2EDCB3A55300010080A501736B7968" +
        "6F73742E646B000010000128AD01000030CB";

    [Fact]
    public void ParseBeaconList_OfficialExample_DecodesAllEightBeacons()
    {
        var list = BeaconParser.ParseBeaconList(HexToBytes(BeaconListHex));

        Assert.Equal(1, list.CurrentPart);
        Assert.Equal(1, list.TotalParts);
        Assert.Equal(8, list.Beacons.Count);

        var first = Assert.IsType<IBeacon>(list.Beacons[0]);
        Assert.Equal(Guid.Parse("4B5C049F-5153-41FC-A950-D2C264414E10"), first.Uuid);
        Assert.Equal(5, first.Major);
        Assert.Equal(6, first.Minor);
        Assert.Equal((sbyte?)-70, first.Rssi);
        Assert.Null(first.BatteryVoltage);
        Assert.Null(first.Temperature);

        var second = Assert.IsType<IBeacon>(list.Beacons[1]);
        Assert.Equal(Guid.Parse("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0"), second.Uuid);
        Assert.Equal((sbyte?)-87, second.Rssi);

        var third = Assert.IsType<IBeacon>(list.Beacons[2]);
        Assert.Equal(45, third.Major);
        Assert.Equal(21, third.Minor);
        Assert.Equal((sbyte?)-97, third.Rssi);

        var fourth = Assert.IsType<Eddystone>(list.Beacons[3]);
        Assert.Equal(HexToBytes("00112233445566778899"), fourth.Namespace.ToArray());
        Assert.Equal(HexToBytes("ABCDE0810047"), fourth.InstanceId.ToArray());
        Assert.Equal((sbyte?)-82, fourth.Rssi);
        Assert.Equal((ushort)3048, fourth.BatteryVoltage);
        Assert.Equal((short)21, fourth.Temperature);

        var fifth = Assert.IsType<IBeacon>(list.Beacons[4]);
        Assert.Equal(0x15B3, fifth.Major);
        Assert.Equal(0x1A0A, fifth.Minor);
        Assert.Equal((sbyte?)-95, fifth.Rssi);

        var sixth = Assert.IsType<Eddystone>(list.Beacons[5]);
        Assert.Equal(HexToBytes("DE9C18E92CA5AA689697"), sixth.Namespace.ToArray());
        Assert.Equal((sbyte?)-70, sixth.Rssi);

        var seventh = Assert.IsType<IBeacon>(list.Beacons[6]);
        Assert.Equal(1, seventh.Major);
        Assert.Equal(128, seventh.Minor);
        Assert.Equal((sbyte?)-91, seventh.Rssi);

        var eighth = Assert.IsType<Eddystone>(list.Beacons[7]);
        Assert.Equal("skyhost.dk"u8.ToArray(), eighth.Namespace.ToArray());
        Assert.Equal(HexToBytes("000010000128"), eighth.InstanceId.ToArray());
        Assert.Equal((sbyte?)-83, eighth.Rssi);
    }

    [Fact]
    public void ParseBeaconList_EmptyPayload_ReturnsEmptyList()
    {
        var list = BeaconParser.ParseBeaconList(ReadOnlyMemory<byte>.Empty);
        Assert.Empty(list.Beacons);
    }

    [Fact]
    public void ParseBeaconList_DataPartByteOnly_ReturnsEmptyList()
    {
        var list = BeaconParser.ParseBeaconList(new byte[] { 0x12 });
        Assert.Equal(1, list.CurrentPart);
        Assert.Equal(2, list.TotalParts);
        Assert.Empty(list.Beacons);
    }

    [Fact]
    public void ParseBeaconList_TruncatedRecord_Throws()
    {
        // iBeacon flags followed by only 4 of the 20 required ID bytes
        var payload = HexToBytes("112101020304");
        Assert.Throws<InvalidDataException>(() => BeaconParser.ParseBeaconList(payload));
    }

    [Fact]
    public void TryParseBeaconList_TruncatedRecord_ReturnsFalse()
    {
        Assert.False(BeaconParser.TryParseBeaconList(HexToBytes("112101020304"), out var list));
        Assert.Null(list);

        Assert.True(BeaconParser.TryParseBeaconList(HexToBytes(BeaconListHex), out list));
        Assert.Equal(8, list!.Beacons.Count);
    }

    [Fact]
    public void ParseAdvancedBeacons_OfficialExample_DecodesAllFourBeacons()
    {
        var beacons = BeaconParser.ParseAdvancedBeacons(HexToBytes(AdvancedBeaconHex));

        Assert.Equal(4, beacons.Count);

        Assert.Equal((sbyte?)-79, beacons[0].Rssi);
        Assert.Equal(HexToBytes("F34B6F6AA38255AA9EF619154E2D0055"), beacons[0].BeaconId.ToArray());
        Assert.Equal(
            HexToBytes("0201060303AAFE1716AAFE0002F34B6F6AA38255AA9EF619154E2D00550000"),
            beacons[0].AdditionalData.ToArray());

        Assert.Equal((sbyte?)-85, beacons[1].Rssi);
        Assert.Equal(HexToBytes("E987706AA38255AA94321B154E2D0055"), beacons[1].BeaconId.ToArray());

        Assert.Equal((sbyte?)-88, beacons[2].Rssi);
        Assert.Equal(HexToBytes("1E74706AA38255FAABCD000000000000"), beacons[2].BeaconId.ToArray());

        Assert.Equal((sbyte?)-94, beacons[3].Rssi);
        Assert.Equal(HexToBytes("0C8C6F6BA38255AAB7361A164E2D0055"), beacons[3].BeaconId.ToArray());
        Assert.Equal(
            HexToBytes("0201060303AAFE1716AAFE00020C8C6F6BA38255AAB7361A164E2D00550000"),
            beacons[3].AdditionalData.ToArray());
    }

    [Fact]
    public void ParseAdvancedBeacons_EmptyPayload_ReturnsEmptyList()
    {
        Assert.Empty(BeaconParser.ParseAdvancedBeacons(ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void ParseAdvancedBeacons_RecordLengthOverrunsPayload_Throws()
    {
        // header + record claiming 0x36 bytes with only 2 present
        var payload = HexToBytes("01360001");
        Assert.Throws<InvalidDataException>(() => BeaconParser.ParseAdvancedBeacons(payload));
    }

    [Fact]
    public void TryParseAdvancedBeacons_Malformed_ReturnsFalse()
    {
        Assert.False(BeaconParser.TryParseAdvancedBeacons(HexToBytes("01360001"), out var beacons));
        Assert.Null(beacons);
    }

    [Fact]
    public void GetBeacons_FullCodec8ExtendedPacket_ParsesEndToEnd()
    {
        var packet = AvlParser.Parse(HexToBytes(BeaconPacketHex));

        var record = Assert.Single(packet.Records);
        Assert.Equal(385, record.IoData.EventId);

        var list = BeaconParser.GetBeacons(record.IoData);
        Assert.NotNull(list);
        Assert.Equal(8, list.Beacons.Count);
        Assert.Equal(Guid.Parse("4B5C049F-5153-41FC-A950-D2C264414E10"), Assert.IsType<IBeacon>(list.Beacons[0]).Uuid);
    }

    [Fact]
    public void GetBeacons_ElementWithoutBeaconProperty_ReturnsNull()
    {
        var element = new IoElement(1, [new IoProperty(21, new byte[] { 0x05 })]);
        Assert.Null(BeaconParser.GetBeacons(element));
        Assert.Null(BeaconParser.GetAdvancedBeacons(element));
    }

    [Fact]
    public void GetAdvancedBeacons_ElementWithProperty548_Parses()
    {
        var element = new IoElement(548, [new IoProperty(548, HexToBytes(AdvancedBeaconHex))]);
        var beacons = BeaconParser.GetAdvancedBeacons(element);
        Assert.NotNull(beacons);
        Assert.Equal(4, beacons.Count);
    }
}
