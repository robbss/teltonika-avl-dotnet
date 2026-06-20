using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Codecs;

public sealed class Codec8ExtendedEncoder : IDataCodecEncoder
{
    public static readonly Codec8ExtendedEncoder Instance = new();

    public byte[] EncodeDataPacket(AvlPacket packet)
    {
        var dataField = BuildDataField(packet);
        return PacketFramer.Frame(dataField);
    }

    private static byte[] BuildDataField(AvlPacket packet)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x8E);
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

        var group1 = new List<IoProperty>();
        var group2 = new List<IoProperty>();
        var group4 = new List<IoProperty>();
        var group8 = new List<IoProperty>();
        var groupVar = new List<IoProperty>();

        foreach (var prop in io.Properties)
        {
            switch (prop.Value.Length)
            {
                case 1: group1.Add(prop); break;
                case 2: group2.Add(prop); break;
                case 4: group4.Add(prop); break;
                case 8: group8.Add(prop); break;
                default: groupVar.Add(prop); break;
            }
        }

        Codec8Encoder.WriteUInt16BE(ms, (ushort)io.Properties.Count);
        WriteIoGroup(ms, group1);
        WriteIoGroup(ms, group2);
        WriteIoGroup(ms, group4);
        WriteIoGroup(ms, group8);
        WriteVariableLengthGroup(ms, groupVar);
    }

    private static void WriteIoGroup(MemoryStream ms, List<IoProperty> group)
    {
        Codec8Encoder.WriteUInt16BE(ms, (ushort)group.Count);
        foreach (var prop in group)
        {
            Codec8Encoder.WriteUInt16BE(ms, prop.Id);
            ms.Write(prop.Value.Span);
        }
    }

    private static void WriteVariableLengthGroup(MemoryStream ms, List<IoProperty> group)
    {
        Codec8Encoder.WriteUInt16BE(ms, (ushort)group.Count);
        foreach (var prop in group)
        {
            Codec8Encoder.WriteUInt16BE(ms, prop.Id);
            Codec8Encoder.WriteUInt16BE(ms, (ushort)prop.Value.Length);
            ms.Write(prop.Value.Span);
        }
    }
}
