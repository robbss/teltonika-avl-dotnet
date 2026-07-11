using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec16Encoder : IDataCodecEncoder
{
    public static readonly Codec16Encoder Instance = new();

    public byte[] EncodeDataPacket(AvlPacket packet)
    {
        int dataLength = MeasureDataField(packet);
        var buffer = PacketFramer.AllocatePacket(dataLength);
        var writer = new SpanWriter(buffer.AsSpan(8, dataLength));

        writer.WriteByte(0x10);
        writer.WriteByte((byte)packet.Records.Count);

        foreach (var record in packet.Records)
            WriteRecord(ref writer, record);

        writer.WriteByte((byte)packet.Records.Count);

        PacketFramer.SealPacket(buffer);
        return buffer;
    }

    private static int MeasureDataField(AvlPacket packet)
    {
        int size = 3; // codec id + record count x2
        foreach (var record in packet.Records)
        {
            size += 24; // timestamp + priority + gps
            size += 4 + 8; // event id + generation type + total count + 4 groups of (generation type + count)
            foreach (var prop in record.IoData.Properties)
            {
                int length = prop.Value.Length;
                if (length is not (1 or 2 or 4 or 8))
                    throw new ArgumentException($"Codec 16 does not support IO property with value length {length} (ID={prop.Id}). Use Codec 8 Extended.");
                size += 2 + length;
            }
        }
        return size;
    }

    private static void WriteRecord(ref SpanWriter writer, AvlRecord record)
    {
        writer.WriteInt64(record.Timestamp.ToUnixTimeMilliseconds());
        writer.WriteByte((byte)record.Priority);
        Codec8Encoder.WriteGps(ref writer, record.Gps);
        WriteIoElement(ref writer, record.IoData);
    }

    private static void WriteIoElement(ref SpanWriter writer, IoElement io)
    {
        writer.WriteUInt16(io.EventId);

        // Generation type byte (not stored in model, default 0x00)
        writer.WriteByte(0x00);

        writer.WriteByte((byte)io.Properties.Count);
        WriteIoGroup(ref writer, io, 1);
        WriteIoGroup(ref writer, io, 2);
        WriteIoGroup(ref writer, io, 4);
        WriteIoGroup(ref writer, io, 8);
    }

    private static void WriteIoGroup(ref SpanWriter writer, IoElement io, int valueSize)
    {
        int count = 0;
        foreach (var prop in io.Properties)
        {
            if (prop.Value.Length == valueSize)
                count++;
        }

        // Generation type byte per group
        writer.WriteByte(0x00);
        writer.WriteByte((byte)count);
        foreach (var prop in io.Properties)
        {
            if (prop.Value.Length != valueSize)
                continue;

            writer.WriteUInt16(prop.Id);
            writer.WriteBytes(prop.Value.Span);
        }
    }
}
