using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec8ExtendedEncoder : IDataCodecEncoder
{
    public static readonly Codec8ExtendedEncoder Instance = new();

    public byte[] EncodeDataPacket(AvlPacket packet)
    {
        int dataLength = MeasureDataField(packet);
        var buffer = PacketFramer.AllocatePacket(dataLength);
        var writer = new SpanWriter(buffer.AsSpan(8, dataLength));

        writer.WriteByte(0x8E);
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
            size += 4 + 10; // event id + total count + 5 group counts
            foreach (var prop in record.IoData.Properties)
            {
                int length = prop.Value.Length;
                size += 2 + length;
                if (length is not (1 or 2 or 4 or 8))
                    size += 2; // variable-length group entries carry a length prefix
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
        writer.WriteUInt16((ushort)io.Properties.Count);
        WriteIoGroup(ref writer, io, 1);
        WriteIoGroup(ref writer, io, 2);
        WriteIoGroup(ref writer, io, 4);
        WriteIoGroup(ref writer, io, 8);
        WriteVariableLengthGroup(ref writer, io);
    }

    private static void WriteIoGroup(ref SpanWriter writer, IoElement io, int valueSize)
    {
        int count = 0;
        foreach (var prop in io.Properties)
        {
            if (prop.Value.Length == valueSize)
                count++;
        }

        writer.WriteUInt16((ushort)count);
        foreach (var prop in io.Properties)
        {
            if (prop.Value.Length != valueSize)
                continue;

            writer.WriteUInt16(prop.Id);
            writer.WriteBytes(prop.Value.Span);
        }
    }

    private static void WriteVariableLengthGroup(ref SpanWriter writer, IoElement io)
    {
        int count = 0;
        foreach (var prop in io.Properties)
        {
            if (prop.Value.Length is not (1 or 2 or 4 or 8))
                count++;
        }

        writer.WriteUInt16((ushort)count);
        foreach (var prop in io.Properties)
        {
            if (prop.Value.Length is 1 or 2 or 4 or 8)
                continue;

            writer.WriteUInt16(prop.Id);
            writer.WriteUInt16((ushort)prop.Value.Length);
            writer.WriteBytes(prop.Value.Span);
        }
    }
}
