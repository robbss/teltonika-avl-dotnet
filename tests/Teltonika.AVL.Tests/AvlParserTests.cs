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