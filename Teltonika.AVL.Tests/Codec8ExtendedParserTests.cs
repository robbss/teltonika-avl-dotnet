using Teltonika.AVL.Models;
using Teltonika.AVL.Protocol;

namespace Teltonika.AVL.Tests;

public class Codec8ExtendedParserTests
{
    [Fact]
    public void Parse_ValidPacket_ParsesSuccessfully()
    {
        string hex = "000000000000004A8E010000016B412CEE000100000000000000000000000000000000010005000100010100010011001D00010010015E2C880002000B000000003544C87A000E000000001DD7E06A00000100002994";
        byte[] fullPacket = ConvertHexStringToByteArray(hex);

        // Payload starts after 8 bytes (preamble + length) and ends before 4 bytes (CRC)
        byte[] payload = fullPacket.Skip(8).Take(fullPacket.Length - 12).ToArray();

        var parser = new Codec8ExtendedParser();
        bool success = parser.TryParse(payload, out var packet);

        Assert.True(success);
        Assert.NotNull(packet);
        Assert.Equal(AvlCodec.Codec8Extended, packet.CodecId);
        Assert.Equal(1, packet.RecordCount);
        Assert.Single(packet.Records);
    }

    private static byte[] ConvertHexStringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
    }
}