using Teltonika.AVL.Elements;

namespace Teltonika.AVL.Tests;

public class AvlParserTests
{
    private readonly AvlParser uut;

    public AvlParserTests()
    {
        uut = new();
    }

    [Theory]
    [InlineData("000000000000003608010000016B40D8EA30010000000000000000000000000000000105021503010101425E0F01F10000601A014E0000000000000000010000C7CF")]
    [InlineData("000000000000002808010000016B40D9AD80010000000000000000000000000000000103021503010101425E100000010000F22A")]
    [InlineData("000000000000004308020000016B40D57B480100000000000000000000000000000001010101000000000000016B40D5C198010000000000000000000000000000000101010101000000020000252C")]
    public void ParseMessage_WithValidCodec8_ReturnsMessage(string hex)
    {
        var bytes = StringToByteArray(hex);
        var memory = new ReadOnlyMemory<byte>(bytes);

        var message = uut.ReadMessage(ref memory, true);

        Assert.NotNull(message);
    }

    [Theory]
    [InlineData("000000000000004A8E010000016B412CEE000100000000000000000000000000000000010005000100010100010011001D00010010015E2C880002000B000000003544C87A000E000000001DD7E06A00000100002994")]
    public void ParseMessage_WithValidCodec8Extended_ReturnsMessage(string hex)
    {
        var bytes = StringToByteArray(hex);
        var memory = new ReadOnlyMemory<byte>(bytes);

        var message = uut.ReadMessage(ref memory, true);

        Assert.NotNull(message);
    }

    [Theory]
    [InlineData("000000000000005F10020000016BDBC7833000000000000000000000000000000000000B05040200010000030002000B00270042563A00000000016BDBC7871800000000000000000000000000000000000B05040200010000030002000B00260042563A00000200005FB3")]
    public void ParseMessage_WithValidCodec16_ReturnsMessage(string hex)
    {
        var bytes = StringToByteArray(hex);
        var memory = new ReadOnlyMemory<byte>(bytes);

        var message = uut.ReadMessage(ref memory, true);

        Assert.NotNull(message);
    }

    [Fact]
    public void ParseMessage_CarstenData()
    {
        var bytes = File.ReadAllBytes(@"C:\Users\RobertConanMcMillan\Downloads\20240401-155053-9b92e07b-7a10-4d1e-9898-8089bd3a06a2(735).data");
        var memory = new ReadOnlyMemory<byte>(bytes);

        var message = uut.ReadMessage(ref memory, true);
        var elements = message.Records.SelectMany(o => AvlElementParser.ParseElements(DeviceType.FMB900, o.Elements)).ToList();

        Assert.NotNull(message);
    }

    [Fact]
    public void Scan_ForBeacons()
    {
        var boy = StringToByteArray("E2C56DB5DFFB48D2B060D0F5A71096E0");

        var root = new DirectoryInfo("D:\\poopoo\\TeltonikaScripts\\Data\\Carsten");
        var files = root.GetFiles("*", new EnumerationOptions() { RecurseSubdirectories = true });

        foreach (var file in files )
        {
            var bytes = File.ReadAllBytes(file.FullName);
            var memory = new ReadOnlyMemory<byte>(bytes);

            var message = uut.ReadMessage(ref memory, true);

            if (message.Records.Any(o => o.Elements.Any(k => k.Id == 385)))
            {
                var elements = message.Records.SelectMany(o => AvlElementParser.ParseElements(DeviceType.FMB900, o.Elements)).ToList();
            }
        }

    }

    private static byte[] StringToByteArray(string hex)
    {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];

        for (int i = 0; i < NumberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }
}