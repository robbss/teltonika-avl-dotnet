using System.Buffers;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal sealed class Codec16Decoder : ICodecDecoder
{
    public static readonly Codec16Decoder Instance = new();

    public bool TryDecodeDataPacket(ref SequenceReader<byte> reader, out AvlPacket? packet, out string? error)
    {
        packet = null;

        if (!reader.TryRead(out _) || !reader.TryRead(out byte recordCount))
        {
            error = Codec8Decoder.TruncatedError;
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
            error = Codec8Decoder.TruncatedError;
            return false;
        }

        if (recordCount != recordCount2)
        {
            error = $"Record count mismatch: {recordCount} != {recordCount2}";
            return false;
        }

        packet = new AvlPacket(CodecId.Codec16, records);
        error = null;
        return true;
    }

    private static bool TryReadRecord(ref SequenceReader<byte> reader, out AvlRecord? record, out string? error)
    {
        record = null;

        if (!Codec8Decoder.TryReadRecordHeader(ref reader, out var timestamp, out var priority, out var gps, out error))
            return false;

        if (!TryReadIoElement(ref reader, out var io, out error))
            return false;

        record = new AvlRecord(timestamp, priority, gps, io!);
        return true;
    }

    private static bool TryReadIoElement(ref SequenceReader<byte> reader, out IoElement? io, out string? error)
    {
        io = null;

        // Codec 16 carries a generation type byte between the event ID and the total count. One per
        // record - not one per group, which is what this used to assume: reading a byte before every
        // group's count consumed four bytes that were not there and derailed the rest of the packet.
        if (!reader.TryReadBigEndian(out short eventIdRaw) ||
            !reader.TryRead(out byte generationType) ||
            !reader.TryRead(out byte totalCount))
        {
            error = Codec8Decoder.TruncatedError;
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

        io = new IoElement((ushort)eventIdRaw, properties, generationType);
        return true;
    }

    private static bool TryReadIoGroup(ref SequenceReader<byte> reader, int valueSize, List<IoProperty> properties, out string? error)
    {
        // Same group shape as Codec 8: a count, then id/value pairs.
        if (!reader.TryRead(out byte count))
        {
            error = Codec8Decoder.TruncatedError;
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            var value = new byte[valueSize];
            if (!reader.TryReadBigEndian(out short idRaw) || !reader.TryCopyTo(value))
            {
                error = Codec8Decoder.TruncatedError;
                return false;
            }
            reader.Advance(valueSize);
            properties.Add(new IoProperty((ushort)idRaw, value));
        }

        error = null;
        return true;
    }
}
