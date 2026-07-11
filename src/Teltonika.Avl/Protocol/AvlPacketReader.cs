using System.Buffers;

namespace Teltonika.Avl.Protocol;

public static class AvlPacketReader
{
    private const int PreambleSize = 4;
    private const int DataLengthSize = 4;
    private const int CrcSize = 4;
    private const int HeaderSize = PreambleSize + DataLengthSize;

    /// <summary>
    /// Reads one framed packet. Returns false when more data is needed;
    /// throws <see cref="InvalidDataException"/> when the buffered data is corrupt.
    /// </summary>
    public static bool TryReadPacket(
        in ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> dataField,
        out SequencePosition consumed,
        out SequencePosition examined)
    {
        if (TryReadPacket(in buffer, out dataField, out consumed, out examined, out var error))
            return true;

        return error is null ? false : throw new InvalidDataException(error);
    }

    /// <summary>
    /// Reads one framed packet without throwing. Returns false with <paramref name="error"/>
    /// set when the buffered data is corrupt (bad preamble, length, or CRC), or with
    /// <paramref name="error"/> null when more data is needed.
    /// </summary>
    public static bool TryReadPacket(
        in ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> dataField,
        out SequencePosition consumed,
        out SequencePosition examined,
        out string? error)
    {
        dataField = default;
        consumed = buffer.Start;
        examined = buffer.End;
        error = null;

        if (buffer.Length < HeaderSize + CrcSize)
            return false;

        var reader = new SequenceReader<byte>(buffer);

        // Read and verify preamble (4 zero bytes)
        reader.TryReadBigEndian(out int preamble);
        if (preamble != 0)
        {
            error = "Invalid preamble: expected 0x00000000";
            return false;
        }

        // Read data field length
        reader.TryReadBigEndian(out int dataLength);
        if (dataLength <= 0)
        {
            error = $"Invalid data length: {dataLength}";
            return false;
        }

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
        {
            dataField = default;
            error = $"CRC mismatch: computed 0x{computedCrc:X4}, expected 0x{expectedCrc:X4}";
            return false;
        }

        consumed = buffer.GetPosition(totalPacketSize);
        examined = consumed;
        return true;
    }
}
