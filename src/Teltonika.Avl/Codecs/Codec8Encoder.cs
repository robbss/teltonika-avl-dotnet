using System.Buffers.Binary;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec8Encoder : IDataCodecEncoder
{
    public static readonly Codec8Encoder Instance = new();

    public byte[] EncodeDataPacket(AvlPacket packet)
    {
        var dataField = BuildDataField(packet);
        return PacketFramer.Frame(dataField);
    }

    private static byte[] BuildDataField(AvlPacket packet)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x08);
        ms.WriteByte((byte)packet.Records.Count);

        foreach (var record in packet.Records)
            WriteRecord(ms, record);

        ms.WriteByte((byte)packet.Records.Count);
        return ms.ToArray();
    }

    private static void WriteRecord(MemoryStream ms, AvlRecord record)
    {
        WriteInt64BE(ms, record.Timestamp.ToUnixTimeMilliseconds());
        ms.WriteByte((byte)record.Priority);
        WriteGps(ms, record.Gps);
        WriteIoElement(ms, record.IoData);
    }

    internal static void WriteGps(MemoryStream ms, GpsData gps)
    {
        WriteInt32BE(ms, (int)(gps.Longitude * 10_000_000));
        WriteInt32BE(ms, (int)(gps.Latitude * 10_000_000));
        WriteInt16BE(ms, gps.Altitude);
        WriteInt16BE(ms, (short)gps.Angle);
        ms.WriteByte(gps.Satellites);
        WriteInt16BE(ms, (short)gps.Speed);
    }

    private static void WriteIoElement(MemoryStream ms, IoElement io)
    {
        ms.WriteByte((byte)io.EventId);

        var group1 = new List<IoProperty>();
        var group2 = new List<IoProperty>();
        var group4 = new List<IoProperty>();
        var group8 = new List<IoProperty>();

        foreach (var prop in io.Properties)
        {
            switch (prop.Value.Length)
            {
                case 1: group1.Add(prop); break;
                case 2: group2.Add(prop); break;
                case 4: group4.Add(prop); break;
                case 8: group8.Add(prop); break;
                default:
                    throw new ArgumentException($"Codec 8 does not support IO property with value length {prop.Value.Length} (ID={prop.Id}). Use Codec 8 Extended.");
            }
        }

        ms.WriteByte((byte)io.Properties.Count);
        WriteIoGroup1Byte(ms, group1);
        WriteIoGroup1Byte(ms, group2);
        WriteIoGroup1Byte(ms, group4);
        WriteIoGroup1Byte(ms, group8);
    }

    private static void WriteIoGroup1Byte(MemoryStream ms, List<IoProperty> group)
    {
        ms.WriteByte((byte)group.Count);
        foreach (var prop in group)
        {
            ms.WriteByte((byte)prop.Id);
            ms.Write(prop.Value.Span);
        }
    }

    internal static void WriteInt64BE(MemoryStream ms, long value)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buf, value);
        ms.Write(buf);
    }

    internal static void WriteInt32BE(MemoryStream ms, int value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf, value);
        ms.Write(buf);
    }

    internal static void WriteInt16BE(MemoryStream ms, short value)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buf, value);
        ms.Write(buf);
    }

    internal static void WriteUInt16BE(MemoryStream ms, ushort value)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buf, value);
        ms.Write(buf);
    }
}
