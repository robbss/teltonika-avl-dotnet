using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec16Encoder : IDataCodecEncoder
{
    public static readonly Codec16Encoder Instance = new();

    public byte[] EncodeDataPacket(AvlPacket packet)
    {
        var dataField = BuildDataField(packet);
        return PacketFramer.Frame(dataField);
    }

    private static byte[] BuildDataField(AvlPacket packet)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x10);
        ms.WriteByte((byte)packet.Records.Count);

        foreach (var record in packet.Records)
            WriteRecord(ms, record);

        ms.WriteByte((byte)packet.Records.Count);
        return ms.ToArray();
    }

    private static void WriteRecord(MemoryStream ms, AvlRecord record)
    {
        Codec8Encoder.WriteInt64BE(ms, record.Timestamp.ToUnixTimeMilliseconds());
        ms.WriteByte((byte)record.Priority);
        Codec8Encoder.WriteGps(ms, record.Gps);
        WriteIoElement(ms, record.IoData);
    }

    private static void WriteIoElement(MemoryStream ms, IoElement io)
    {
        Codec8Encoder.WriteUInt16BE(ms, io.EventId);

        // Generation type byte (not stored in model, default 0x00)
        ms.WriteByte(0x00);

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
                    throw new ArgumentException($"Codec 16 does not support IO property with value length {prop.Value.Length} (ID={prop.Id}). Use Codec 8 Extended.");
            }
        }

        ms.WriteByte((byte)io.Properties.Count);
        WriteIoGroup(ms, group1);
        WriteIoGroup(ms, group2);
        WriteIoGroup(ms, group4);
        WriteIoGroup(ms, group8);
    }

    private static void WriteIoGroup(MemoryStream ms, List<IoProperty> group)
    {
        // Generation type byte per group
        ms.WriteByte(0x00);
        ms.WriteByte((byte)group.Count);
        foreach (var prop in group)
        {
            Codec8Encoder.WriteUInt16BE(ms, prop.Id);
            ms.Write(prop.Value.Span);
        }
    }
}
