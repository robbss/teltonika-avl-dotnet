using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec8Encoder : IDataCodecEncoder
{
    public static readonly Codec8Encoder Instance = new();

    public byte[] EncodeDataPacket(AvlPacket packet)
    {
        int dataLength = MeasureDataField(packet);
        var buffer = PacketFramer.AllocatePacket(dataLength);
        var writer = new SpanWriter(buffer.AsSpan(8, dataLength));

        writer.WriteByte(0x08);
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
            size += 2 + 4; // event id + total count + 4 group counts
            foreach (var prop in record.IoData.Properties)
            {
                int length = prop.Value.Length;
                if (length is not (1 or 2 or 4 or 8))
                    throw new ArgumentException($"Codec 8 does not support IO property with value length {length} (ID={prop.Id}). Use Codec 8 Extended.");
                size += 1 + length;
            }
        }
        return size;
    }

    private static void WriteRecord(ref SpanWriter writer, AvlRecord record)
    {
        writer.WriteInt64(record.Timestamp.ToUnixTimeMilliseconds());
        writer.WriteByte((byte)record.Priority);
        WriteGps(ref writer, record.Gps);
        WriteIoElement(ref writer, record.IoData);
    }

    internal static void WriteGps(ref SpanWriter writer, GpsData gps)
    {
        writer.WriteInt32((int)(gps.Longitude * 10_000_000));
        writer.WriteInt32((int)(gps.Latitude * 10_000_000));
        writer.WriteInt16(gps.Altitude);
        writer.WriteInt16((short)gps.Angle);
        writer.WriteByte(gps.Satellites);
        writer.WriteInt16((short)gps.Speed);
    }

    private static void WriteIoElement(ref SpanWriter writer, IoElement io)
    {
        writer.WriteByte((byte)io.EventId);
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

        writer.WriteByte((byte)count);
        foreach (var prop in io.Properties)
        {
            if (prop.Value.Length != valueSize)
                continue;

            writer.WriteByte((byte)prop.Id);
            writer.WriteBytes(prop.Value.Span);
        }
    }
}
