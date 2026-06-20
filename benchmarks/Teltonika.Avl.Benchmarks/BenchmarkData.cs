using Teltonika.Avl;
using Teltonika.Avl.Elements.Models;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Benchmarks;

internal static class BenchmarkData
{
    // Codec 8: 1 record, 9 IO properties
    private static readonly string Codec8DataField =
        "08" +
        "01" +
        "0000016B40D8EA30" +
        "00" +
        "0F0E9D60" +
        "209A7400" +
        "0000" +
        "015E" +
        "0C" +
        "0000" +
        "01" +
        "09" +
        "01" +
        "01" + "01" +
        "02" +
        "02" + "0001" +
        "03" + "0003" +
        "03" +
        "04" + "00000001" +
        "05" + "00000002" +
        "06" + "00000003" +
        "03" +
        "07" + "0000000000000001" +
        "08" + "0000000000000002" +
        "09" + "0000000000000003" +
        "01";

    // Codec 8 Extended: 1 record, 2-byte IO IDs, variable-length element
    private static readonly string Codec8ExtDataField =
        "8E" +
        "01" +
        "0000016B40D9AD80" +
        "00" +
        "0F0E9D60" +
        "209A7400" +
        "0000" +
        "0000" +
        "00" +
        "0000" +
        "0001" +
        "0005" +
        "0001" +
        "0001" + "01" +
        "0001" +
        "0002" + "0005" +
        "0001" +
        "0003" + "00000064" +
        "0001" +
        "0004" + "00000000000003E8" +
        "0001" +
        "0005" + "0003" + "414243" +
        "01";

    // Codec 16: 1 record, 2-byte IO IDs, generation type bytes
    private static readonly string Codec16DataField =
        "10" +                    // codec 16
        "01" +                    // 1 record
        "0000016B40D8EA30" +      // timestamp
        "00" +                    // priority
        "0F0E9D60" +              // longitude
        "209A7400" +              // latitude
        "0000" +                  // altitude
        "015E" +                  // angle
        "0C" +                    // satellites
        "0000" +                  // speed
        "0001" +                  // event id (2 bytes)
        "01" +                    // generation type
        "04" +                    // total IO count
        "01" +                    // 1-byte group generation type
        "01" +                    // 1-byte IO count
        "0001" + "01" +           // id=1 (2 bytes), val=1
        "01" +                    // 2-byte group generation type
        "01" +                    // 2-byte IO count
        "0002" + "0005" +         // id=2, val=5
        "01" +                    // 4-byte group generation type
        "01" +                    // 4-byte IO count
        "0003" + "00000064" +     // id=3, val=100
        "01" +                    // 8-byte group generation type
        "01" +                    // 8-byte IO count
        "0004" + "00000000000003E8" + // id=4, val=1000
        "01";                     // record count 2

    // Codec 12 command request
    private static readonly string Codec12DataField =
        "0C" +
        "01" +
        "05" +
        "00000007" +
        "676574696E666F" +
        "01";

    // IMEI frame
    private static readonly string ImeiHex =
        "000F" +
        "333536333037303432343431303133";

    public static readonly byte[] Codec8Packet = BuildPacket(Codec8DataField);
    public static readonly byte[] Codec8ExtPacket = BuildPacket(Codec8ExtDataField);
    public static readonly byte[] Codec16Packet = BuildPacket(Codec16DataField);
    public static readonly byte[] Codec12Packet = BuildPacket(Codec12DataField);
    public static readonly byte[] ImeiFrame = HexToBytes(ImeiHex);

    public static readonly AvlPacket ParsedCodec8Packet = AvlParser.Parse(Codec8Packet);
    public static readonly AvlPacket ParsedCodec8ExtPacket = AvlParser.Parse(Codec8ExtPacket);
    public static readonly AvlPacket ParsedCodec16Packet = AvlParser.Parse(Codec16Packet);

    public static readonly GprsCommandPacket ParsedCommandPacket = AvlParser.ParseCommand(Codec12Packet);

    public static readonly byte[] CrcSmallPayload = HexToBytes(Codec8DataField);
    public static readonly byte[] CrcLargePayload = CreateLargePayload(1024);

    public static readonly IoProperty KnownProperty = new(66, new byte[] { 0x2E, 0xE0 });
    public static readonly IoProperty OverrideProperty = new(389, new byte[] { 18 });
    public static readonly IoProperty UnknownProperty = new(9999, new byte[] { 0x01 });

    public static readonly IoElement SmallIoElement = new(0, new IoProperty[]
    {
        new(66, new byte[] { 0x2E, 0xE0 }),   // External Voltage
        new(239, new byte[] { 0x01 }),          // Ignition
        new(240, new byte[] { 0x00 }),          // Movement
        new(21, new byte[] { 0x03 }),           // GSM Signal
    });

    public static readonly IoElement LargeIoElement = new(0, new IoProperty[]
    {
        new(66, new byte[] { 0x2E, 0xE0 }),
        new(67, new byte[] { 0x10, 0x68 }),
        new(239, new byte[] { 0x01 }),
        new(240, new byte[] { 0x00 }),
        new(21, new byte[] { 0x03 }),
        new(200, new byte[] { 0x00 }),
        new(69, new byte[] { 0x02 }),
        new(181, new byte[] { 0x00, 0x0F }),
        new(182, new byte[] { 0x00, 0x0A }),
        new(24, new byte[] { 0x00, 0x64 }),
        new(205, new byte[] { 0x00, 0x0E }),
        new(206, new byte[] { 0x00, 0x0D }),
        new(80, new byte[] { 0x01 }),
        new(253, new byte[] { 0x02 }),
        new(1, new byte[] { 0x00, 0x00, 0x01, 0x00 }),
        new(9, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }),
        new(9998, new byte[] { 0xFF }),
        new(9999, new byte[] { 0xAB }),
        new(16, new byte[] { 0x00, 0x00, 0x13, 0x88 }),
        new(11, new byte[] { 0x00, 0xFA }),
    });

    private static byte[] HexToBytes(string hex)
    {
        hex = hex.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    private static byte[] BuildPacket(string dataFieldHex)
    {
        var dataBytes = HexToBytes(dataFieldHex);
        ushort crc = Crc16Ibm.Compute(dataBytes);

        var packet = new byte[4 + 4 + dataBytes.Length + 4];
        int len = dataBytes.Length;
        packet[4] = (byte)(len >> 24);
        packet[5] = (byte)(len >> 16);
        packet[6] = (byte)(len >> 8);
        packet[7] = (byte)len;
        dataBytes.CopyTo(packet, 8);
        int crcOffset = 8 + dataBytes.Length;
        packet[crcOffset + 2] = (byte)(crc >> 8);
        packet[crcOffset + 3] = (byte)crc;

        return packet;
    }

    private static byte[] CreateLargePayload(int size)
    {
        var payload = new byte[size];
        for (int i = 0; i < size; i++)
            payload[i] = (byte)(i & 0xFF);
        return payload;
    }
}
