using System.Buffers;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal sealed class Codec8Decoder : ICodecDecoder
{
    public static readonly Codec8Decoder Instance = new();

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

        return new AvlPacket(CodecId.Codec8, records);
    }

    private static AvlRecord ReadRecord(ref SequenceReader<byte> reader)
    {
        reader.TryReadBigEndian(out long timestampMs);
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);

        reader.TryRead(out byte priority);

        var gps = ReadGps(ref reader);
        var io = ReadIoElement(ref reader);

        return new AvlRecord(timestamp, (Priority)priority, gps, io);
    }

    internal static GpsData ReadGps(ref SequenceReader<byte> reader)
    {
        reader.TryReadBigEndian(out int longitude);
        reader.TryReadBigEndian(out int latitude);
        reader.TryReadBigEndian(out short altitude);
        reader.TryReadBigEndian(out short angleRaw);
        ushort angle = (ushort)angleRaw;
        reader.TryRead(out byte satellites);
        reader.TryReadBigEndian(out short speedRaw);
        ushort speed = (ushort)speedRaw;

        return new GpsData(
            longitude / 10_000_000.0,
            latitude / 10_000_000.0,
            altitude,
            angle,
            satellites,
            speed);
    }

    private static IoElement ReadIoElement(ref SequenceReader<byte> reader)
    {
        reader.TryRead(out byte eventId);
        reader.TryRead(out byte totalCount);

        var properties = new List<IoProperty>(totalCount);

        ReadIoGroup(ref reader, 1, properties);
        ReadIoGroup(ref reader, 2, properties);
        ReadIoGroup(ref reader, 4, properties);
        ReadIoGroup(ref reader, 8, properties);

        return new IoElement(eventId, properties);
    }

    private static void ReadIoGroup(ref SequenceReader<byte> reader, int valueSize, List<IoProperty> properties)
    {
        reader.TryRead(out byte count);
        for (int i = 0; i < count; i++)
        {
            reader.TryRead(out byte id);
            var value = new byte[valueSize];
            reader.TryCopyTo(value);
            reader.Advance(valueSize);
            properties.Add(new IoProperty(id, value));
        }
    }
}
