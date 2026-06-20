using System.Buffers;
using Teltonika.Avl.Protocol;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Protocol;

public class AvlPacketReaderTests
{
    [Fact]
    public void TryReadPacket_CompletePacket_ReturnsTrue()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec8DataField);
        var seq = new ReadOnlySequence<byte>(packet);

        bool result = AvlPacketReader.TryReadPacket(in seq, out var dataField, out _, out _);

        Assert.True(result);
        Assert.True(dataField.Length > 0);
    }

    [Fact]
    public void TryReadPacket_IncompletePacket_ReturnsFalse()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec8DataField);
        // Truncate packet
        var truncated = packet.AsSpan(0, packet.Length - 10).ToArray();
        var seq = new ReadOnlySequence<byte>(truncated);

        bool result = AvlPacketReader.TryReadPacket(in seq, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryReadPacket_CorruptCrc_Throws()
    {
        var packet = SamplePackets.BuildPacket(SamplePackets.Codec8DataField);
        // Corrupt the CRC
        packet[^1] ^= 0xFF;
        var seq = new ReadOnlySequence<byte>(packet);

        Assert.Throws<InvalidDataException>(() =>
            AvlPacketReader.TryReadPacket(in seq, out _, out _, out _));
    }

    [Fact]
    public void TryReadPacket_TooShort_ReturnsFalse()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };
        var seq = new ReadOnlySequence<byte>(data);

        bool result = AvlPacketReader.TryReadPacket(in seq, out _, out _, out _);

        Assert.False(result);
    }
}
