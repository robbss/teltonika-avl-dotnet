namespace Teltonika.Avl.Tests.TestData;

internal static class SamplePackets
{
    public static byte[] HexToBytes(string hex)
    {
        hex = hex.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    public static byte[] BuildPacket(string dataFieldHex)
    {
        var dataBytes = HexToBytes(dataFieldHex);
        ushort crc = Teltonika.Avl.Protocol.Crc16Ibm.Compute(dataBytes);

        var packet = new byte[4 + 4 + dataBytes.Length + 4];
        // Preamble: 4 zero bytes
        // Data length
        int len = dataBytes.Length;
        packet[4] = (byte)(len >> 24);
        packet[5] = (byte)(len >> 16);
        packet[6] = (byte)(len >> 8);
        packet[7] = (byte)len;
        // Data field
        dataBytes.CopyTo(packet, 8);
        // CRC (4 bytes, lower 16 bits)
        int crcOffset = 8 + dataBytes.Length;
        packet[crcOffset + 2] = (byte)(crc >> 8);
        packet[crcOffset + 3] = (byte)crc;

        return packet;
    }

    // Codec 8: 1 record, GPS at ~25.3032016 / 54.7146368, IO event=1, 9 IO properties
    public static readonly string Codec8DataField =
        "08" +                // codec 8
        "01" +                // 1 record
        "0000016B40D8EA30" +  // timestamp (2019-06-10T10:04:46.000Z)
        "00" +                // priority low
        "0F0E9D60" +          // longitude (252626272 = 25.2626272)
        "209A7400" +          // latitude (547601408 = 54.7601408)
        "0000" +              // altitude 0
        "015E" +              // angle 350
        "0C" +                // satellites 12
        "0000" +              // speed 0
        "01" +                // IO event id = 1
        "09" +                // total IO count = 9
        "01" +                // 1-byte IO count = 1
        "01" + "01" +         // id=1, val=1
        "02" +                // 2-byte IO count = 2
        "02" + "0001" +       // id=2, val=1
        "03" + "0003" +       // id=3, val=3
        "03" +                // 4-byte IO count = 3
        "04" + "00000001" +   // id=4, val=1
        "05" + "00000002" +   // id=5, val=2
        "06" + "00000003" +   // id=6, val=3
        "03" +                // 8-byte IO count = 3
        "07" + "0000000000000001" + // id=7
        "08" + "0000000000000002" + // id=8
        "09" + "0000000000000003" + // id=9
        "01";                 // record count 2 = 1

    // Codec 8 Extended: 1 record, same GPS, 2-byte IO IDs, includes variable-length element
    public static readonly string Codec8ExtDataField =
        "8E" +                // codec 8 extended
        "01" +                // 1 record
        "0000016B40D9AD80" +  // timestamp
        "00" +                // priority low
        "0F0E9D60" +          // longitude
        "209A7400" +          // latitude
        "0000" +              // altitude
        "0000" +              // angle
        "00" +                // satellites
        "0000" +              // speed
        "0001" +              // IO event id = 1 (2 bytes)
        "0005" +              // total IO count = 5 (2 bytes)
        "0001" +              // 1-byte count
        "0001" + "01" +       // id=1, val=1
        "0001" +              // 2-byte count
        "0002" + "0005" +     // id=2, val=5
        "0001" +              // 4-byte count
        "0003" + "00000064" + // id=3, val=100
        "0001" +              // 8-byte count
        "0004" + "00000000000003E8" + // id=4, val=1000
        "0001" +              // variable count
        "0005" + "0003" + "414243" + // id=5, len=3, val="ABC"
        "01";                 // record count 2

    // Codec 12 response: type=0x06, command="OK"
    public static readonly string Codec12ResponseDataField =
        "0C" +                // codec 12
        "01" +                // 1 command
        "06" +                // type = response
        "00000002" +          // command size = 2
        "4F4B" +              // "OK"
        "01";                 // command count 2

    // Codec 12 request: type=0x05, command="getinfo"
    public static readonly string Codec12RequestDataField =
        "0C" +                // codec 12
        "01" +                // 1 command
        "05" +                // type = request
        "00000007" +          // command size = 7
        "676574696E666F" +    // "getinfo"
        "01";                 // command count 2

    // IMEI frame: length=15, IMEI=356307042441013
    public static readonly string ImeiHex =
        "000F" +              // length = 15
        "333536333037303432343431303133"; // "356307042441013" ASCII
}
