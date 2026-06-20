using System.Buffers;

namespace Teltonika.Avl.Protocol;

public static class AvlPacketReader
{
    private const int PreambleSize = 4;
    private const int DataLengthSize = 4;
    private const int CrcSize = 4;
    private const int HeaderSize = PreambleSize + DataLengthSize;

    public static bool TryReadPacket(
        in ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> dataField,
        out SequencePosition consumed,
        out SequencePosition examined)
    {
        dataField = default;
        consumed = buffer.Start;
        examined = buffer.End;

        if (buffer.Length < HeaderSize + CrcSize)
            return false;

        var reader = new SequenceReader<byte>(buffer);

        // Read and verify preamble (4 zero bytes)
        reader.TryReadBigEndian(out int preamble);
        if (preamble != 0)
            throw new InvalidDataException("Invalid preamble: expected 0x00000000");

        // Read data field length
        reader.TryReadBigEndian(out int dataLength);
        if (dataLength <= 0)
            throw new InvalidDataException($"Invalid data length: {dataLength}");

        long totalPacketSize = HeaderSize + dataLength + CrcSize;
        if (buffer.Length < totalPacketSize)
            return false;

        // Extract data field for CRC validation
        dataField = buffer.Slice(HeaderSize, dataLength);

        // Validate CRC
        ushort computedCrc = Crc16Ibm.Compute(in dataField);

        // Read the 4-byte CRC from the packet (only lower 16 bits are significant)
        reader.Advance(dataLength);
        reader.TryReadBigEndian(out int packetCrc);
        ushort expectedCrc = (ushort)(packetCrc & 0xFFFF);

        if (computedCrc != expectedCrc)
            throw new InvalidDataException($"CRC mismatch: computed 0x{computedCrc:X4}, expected 0x{expectedCrc:X4}");

        consumed = buffer.GetPosition(totalPacketSize);
        examined = consumed;
        return true;
    }
}
