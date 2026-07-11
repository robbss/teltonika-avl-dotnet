using System.Buffers;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal sealed class Codec8Decoder : ICodecDecoder
{
    public static readonly Codec8Decoder Instance = new();

    internal const string TruncatedError = "Unexpected end of data field";

    // Valid range of DateTimeOffset.FromUnixTimeMilliseconds
    private const long MaxUnixTimeMilliseconds = 253_402_300_799_999;

    public bool TryDecodeDataPacket(ref SequenceReader<byte> reader, out AvlPacket? packet, out string? error)
    {
        packet = null;

        if (!reader.TryRead(out _) || !reader.TryRead(out byte recordCount))
        {
            error = TruncatedError;
            return false;
        }

        var records = new AvlRecord[recordCount];
        for (int i = 0; i < recordCount; i++)
        {
            if (!TryReadRecord(ref reader, out var record, out error))
                return false;
            records[i] = record!;
        }

        if (!reader.TryRead(out byte recordCount2))
        {
            error = TruncatedError;
            return false;
        }

        if (recordCount != recordCount2)
        {
            error = $"Record count mismatch: {recordCount} != {recordCount2}";
            return false;
        }

        packet = new AvlPacket(CodecId.Codec8, records);
        error = null;
        return true;
    }

    private static bool TryReadRecord(ref SequenceReader<byte> reader, out AvlRecord? record, out string? error)
    {
        record = null;

        if (!TryReadRecordHeader(ref reader, out var timestamp, out var priority, out var gps, out error))
            return false;

        if (!TryReadIoElement(ref reader, out var io, out error))
            return false;

        record = new AvlRecord(timestamp, priority, gps, io!);
        return true;
    }

    /// <summary>Reads the timestamp, priority, and GPS element shared by all data codecs.</summary>
    internal static bool TryReadRecordHeader(
        ref SequenceReader<byte> reader,
        out DateTimeOffset timestamp,
        out Priority priority,
        out GpsData gps,
        out string? error)
    {
        timestamp = default;
        priority = default;
        gps = default;

        if (!reader.TryReadBigEndian(out long timestampMs) || !reader.TryRead(out byte priorityByte))
        {
            error = TruncatedError;
            return false;
        }

        if (timestampMs is < 0 or > MaxUnixTimeMilliseconds)
        {
            error = $"Invalid record timestamp: {timestampMs}";
            return false;
        }

        if (!reader.TryReadBigEndian(out int longitude) ||
            !reader.TryReadBigEndian(out int latitude) ||
            !reader.TryReadBigEndian(out short altitude) ||
            !reader.TryReadBigEndian(out short angleRaw) ||
            !reader.TryRead(out byte satellites) ||
            !reader.TryReadBigEndian(out short speedRaw))
        {
            error = TruncatedError;
            return false;
        }

        timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
        priority = (Priority)priorityByte;
        gps = new GpsData(
            longitude / 10_000_000.0,
            latitude / 10_000_000.0,
            altitude,
            (ushort)angleRaw,
            satellites,
            (ushort)speedRaw);
        error = null;
        return true;
    }

    private static bool TryReadIoElement(ref SequenceReader<byte> reader, out IoElement? io, out string? error)
    {
        io = null;

        if (!reader.TryRead(out byte eventId) || !reader.TryRead(out byte totalCount))
        {
            error = TruncatedError;
            return false;
        }

        var properties = new List<IoProperty>(totalCount);

        if (!TryReadIoGroup(ref reader, 1, properties, out error) ||
            !TryReadIoGroup(ref reader, 2, properties, out error) ||
            !TryReadIoGroup(ref reader, 4, properties, out error) ||
            !TryReadIoGroup(ref reader, 8, properties, out error))
        {
            return false;
        }

        io = new IoElement(eventId, properties);
        return true;
    }

    private static bool TryReadIoGroup(ref SequenceReader<byte> reader, int valueSize, List<IoProperty> properties, out string? error)
    {
        if (!reader.TryRead(out byte count))
        {
            error = TruncatedError;
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            var value = new byte[valueSize];
            if (!reader.TryRead(out byte id) || !reader.TryCopyTo(value))
            {
                error = TruncatedError;
                return false;
            }
            reader.Advance(valueSize);
            properties.Add(new IoProperty(id, value));
        }

        error = null;
        return true;
    }
}
