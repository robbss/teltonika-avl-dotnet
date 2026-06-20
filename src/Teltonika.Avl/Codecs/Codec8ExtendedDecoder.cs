using System.Buffers;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal sealed class Codec8ExtendedDecoder : ICodecDecoder
{
    public static readonly Codec8ExtendedDecoder Instance = new();

    public AvlPacket DecodeDataPacket(ref SequenceReader<byte> reader)
    {
        reader.TryRead(out byte codecByte);
        reader.TryRead(out byte recordCount);

        var records = new AvlRecord[recordCount];
        for (int i = 0; i < recordCount; i++)
            records[i] = ReadRecord(ref reader);

        reader.TryRead(out byte recordCount2);
        if (recordCount != recordCount2)
            throw new InvalidDataException($"Record count mismatch: {recordCount} != {recordCount2}");

        return new AvlPacket(CodecId.Codec8Extended, records);
    }

    private static AvlRecord ReadRecord(ref SequenceReader<byte> reader)
    {
        reader.TryReadBigEndian(out long timestampMs);
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);

        reader.TryRead(out byte priority);

        var gps = Codec8Decoder.ReadGps(ref reader);
        var io = ReadIoElement(ref reader);

        return new AvlRecord(timestamp, (Priority)priority, gps, io);
    }

    private static IoElement ReadIoElement(ref SequenceReader<byte> reader)
    {
        reader.TryReadBigEndian(out short eventIdRaw);
        ushort eventId = (ushort)eventIdRaw;
        reader.TryReadBigEndian(out short totalCountRaw);
        ushort totalCount = (ushort)totalCountRaw;

        var properties = new List<IoProperty>(totalCount);

        ReadIoGroup(ref reader, 1, properties);
        ReadIoGroup(ref reader, 2, properties);
        ReadIoGroup(ref reader, 4, properties);
        ReadIoGroup(ref reader, 8, properties);
        ReadVariableLengthGroup(ref reader, properties);

        return new IoElement(eventId, properties);
    }

    private static void ReadIoGroup(ref SequenceReader<byte> reader, int valueSize, List<IoProperty> properties)
    {
        reader.TryReadBigEndian(out short countRaw);
        ushort count = (ushort)countRaw;
        for (int i = 0; i < count; i++)
        {
            reader.TryReadBigEndian(out short idRaw);
            ushort id = (ushort)idRaw;
            var value = new byte[valueSize];
            reader.TryCopyTo(value);
            reader.Advance(valueSize);
            properties.Add(new IoProperty(id, value));
        }
    }

    private static void ReadVariableLengthGroup(ref SequenceReader<byte> reader, List<IoProperty> properties)
    {
        reader.TryReadBigEndian(out short countRaw);
        ushort count = (ushort)countRaw;
        for (int i = 0; i < count; i++)
        {
            reader.TryReadBigEndian(out short idRaw);
            ushort id = (ushort)idRaw;
            reader.TryReadBigEndian(out short lengthRaw);
            ushort length = (ushort)lengthRaw;
            var value = new byte[length];
            reader.TryCopyTo(value);
            reader.Advance(length);
            properties.Add(new IoProperty(id, value));
        }
    }
}
