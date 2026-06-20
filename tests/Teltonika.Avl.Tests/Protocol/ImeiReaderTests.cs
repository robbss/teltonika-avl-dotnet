using System.Buffers;
using Teltonika.Avl.Protocol;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Protocol;

public class ImeiReaderTests
{
    [Fact]
    public void TryRead_ValidImei_ReturnsTrue()
    {
        var data = SamplePackets.HexToBytes(SamplePackets.ImeiHex);
        var seq = new ReadOnlySequence<byte>(data);

        bool result = ImeiReader.TryRead(in seq, out string? imei, out _);

        Assert.True(result);
        Assert.Equal("356307042441013", imei);
    }

    [Fact]
    public void TryRead_TruncatedData_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x0F, 0x33 }; // length says 15 but only 1 byte
        var seq = new ReadOnlySequence<byte>(data);

        bool result = ImeiReader.TryRead(in seq, out string? imei, out _);

        Assert.False(result);
        Assert.Null(imei);
    }

    [Fact]
    public void TryRead_OnlyLengthBytes_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x0F };
        var seq = new ReadOnlySequence<byte>(data);

        bool result = ImeiReader.TryRead(in seq, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void ParseImei_ViaPublicApi_Works()
    {
        var data = SamplePackets.HexToBytes(SamplePackets.ImeiHex);
        string imei = AvlParser.ParseImei(data);
        Assert.Equal("356307042441013", imei);
    }
}
