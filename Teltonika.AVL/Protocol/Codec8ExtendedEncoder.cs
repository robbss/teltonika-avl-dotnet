using System.Buffers.Binary;
using Teltonika.AVL.Models;

namespace Teltonika.AVL.Protocol;

public class Codec8ExtendedEncoder : ICodecEncoder
{
    public AvlCodec CodecId => AvlCodec.Codec8Extended;

    public byte[] Encode(AvlDataPacket packet)
    {
        if (packet.CodecId != CodecId)
            throw new ArgumentException("Invalid codec for this encoder");

        var dataPayload = new List<byte>();

        // Codec ID
        dataPayload.Add((byte)CodecId);

        // Record Count (1 byte)
        dataPayload.Add((byte)packet.RecordCount);

        foreach (var record in packet.Records)
        {
            // Timestamp (8 bytes)
            var tsBytes = new byte[8];
            BinaryPrimitives.WriteInt64BigEndian(tsBytes, record.Timestamp.ToUnixTimeMilliseconds());
            dataPayload.AddRange(tsBytes);

            // Priority (1 byte)
            dataPayload.Add((byte)record.Priority);

            // GPS
            var lonBytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lonBytes, (int)(record.Gps.Longitude * 10000000.0));
            dataPayload.AddRange(lonBytes);

            var latBytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(latBytes, (int)(record.Gps.Latitude * 10000000.0));
            dataPayload.AddRange(latBytes);

            var altBytes = new byte[2];
            BinaryPrimitives.WriteInt16BigEndian(altBytes, record.Gps.Altitude);
            dataPayload.AddRange(altBytes);

            var angleBytes = new byte[2];
            BinaryPrimitives.WriteInt16BigEndian(angleBytes, record.Gps.Angle);
            dataPayload.AddRange(angleBytes);

            dataPayload.Add(record.Gps.Satellites);

            var speedBytes = new byte[2];
            BinaryPrimitives.WriteInt16BigEndian(speedBytes, record.Gps.Speed);
            dataPayload.AddRange(speedBytes);

            // IO Events
            var eventIdBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(eventIdBytes, (ushort)record.Io.EventId);
            dataPayload.AddRange(eventIdBytes);

            var totalIoBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(totalIoBytes, (ushort)record.Io.TotalIoCount);
            dataPayload.AddRange(totalIoBytes);

            var props1 = record.Io.Properties.Where(p => p.Value.Length == 1).ToList();
            var props2 = record.Io.Properties.Where(p => p.Value.Length == 2).ToList();
            var props4 = record.Io.Properties.Where(p => p.Value.Length == 4).ToList();
            var props8 = record.Io.Properties.Where(p => p.Value.Length == 8).ToList();
            var propsX = record.Io.Properties.Where(p => p.Value.Length != 1 && p.Value.Length != 2 && p.Value.Length != 4 && p.Value.Length != 8).ToList();

            // 1-byte
            var n1Bytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(n1Bytes, (ushort)props1.Count);
            dataPayload.AddRange(n1Bytes);
            foreach (var p in props1)
            {
                var idBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(idBytes, (ushort)p.Key);
                dataPayload.AddRange(idBytes);
                dataPayload.AddRange(p.Value);
            }

            // 2-byte
            var n2Bytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(n2Bytes, (ushort)props2.Count);
            dataPayload.AddRange(n2Bytes);
            foreach (var p in props2)
            {
                var idBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(idBytes, (ushort)p.Key);
                dataPayload.AddRange(idBytes);
                dataPayload.AddRange(p.Value);
            }

            // 4-byte
            var n4Bytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(n4Bytes, (ushort)props4.Count);
            dataPayload.AddRange(n4Bytes);
            foreach (var p in props4)
            {
                var idBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(idBytes, (ushort)p.Key);
                dataPayload.AddRange(idBytes);
                dataPayload.AddRange(p.Value);
            }

            // 8-byte
            var n8Bytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(n8Bytes, (ushort)props8.Count);
            dataPayload.AddRange(n8Bytes);
            foreach (var p in props8)
            {
                var idBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(idBytes, (ushort)p.Key);
                dataPayload.AddRange(idBytes);
                dataPayload.AddRange(p.Value);
            }

            // Variable IO
            var nxBytes = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(nxBytes, (ushort)propsX.Count);
            dataPayload.AddRange(nxBytes);
            foreach (var p in propsX)
            {
                var idBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(idBytes, (ushort)p.Key);
                dataPayload.AddRange(idBytes);

                var lenBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(lenBytes, (ushort)p.Value.Length);
                dataPayload.AddRange(lenBytes);

                dataPayload.AddRange(p.Value);
            }
        }

        // Record Count End
        dataPayload.Add((byte)packet.RecordCount);

        var dataArray = dataPayload.ToArray();

        var finalPacket = new List<byte>();

        // Preamble
        finalPacket.AddRange(new byte[] { 0, 0, 0, 0 });

        // Length
        var lengthBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBytes, dataArray.Length);
        finalPacket.AddRange(lengthBytes);

        // Data
        finalPacket.AddRange(dataArray);

        // CRC
        var crc = Crc16.Compute(dataArray);
        var crcBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, (uint)crc);
        finalPacket.AddRange(crcBytes);

        return finalPacket.ToArray();
    }
}