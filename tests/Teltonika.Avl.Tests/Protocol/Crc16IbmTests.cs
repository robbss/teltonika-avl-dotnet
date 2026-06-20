using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Protocol;

public class Crc16IbmTests
{
    [Fact]
    public void Compute_StandardCheckValue_ReturnsExpectedCrc()
    {
        // CRC-16/ARC (CRC-16/IBM) standard check value for ASCII "123456789"
        var data = "123456789"u8.ToArray();
        ushort crc = Teltonika.Avl.Protocol.Crc16Ibm.Compute(data);
        Assert.Equal(0xBB3D, crc);
    }

    [Fact]
    public void Compute_EmptyInput_ReturnsZero()
    {
        ushort crc = Teltonika.Avl.Protocol.Crc16Ibm.Compute(ReadOnlySpan<byte>.Empty);
        Assert.Equal(0, crc);
    }

    [Fact]
    public void Compute_SingleByte_ReturnsNonZero()
    {
        ushort crc = Teltonika.Avl.Protocol.Crc16Ibm.Compute(new byte[] { 0x42 });
        Assert.NotEqual(0, crc);
    }

    [Fact]
    public void Compute_SequenceAndSpan_ProduceSameResult()
    {
        var data = new byte[] { 0x08, 0x01, 0x02, 0x03, 0x04, 0x05 };
        ushort spanCrc = Teltonika.Avl.Protocol.Crc16Ibm.Compute(data.AsSpan());
        var sequence = new System.Buffers.ReadOnlySequence<byte>(data);
        ushort seqCrc = Teltonika.Avl.Protocol.Crc16Ibm.Compute(in sequence);
        Assert.Equal(spanCrc, seqCrc);
    }
}
