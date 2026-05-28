using System.Buffers.Binary;
using Teltonika.AVL.Models;

namespace Teltonika.AVL.Protocol;

public class Codec8Parser : ICodecParser
{
    public AvlCodec CodecId => AvlCodec.Codec8;

    public bool TryParse(ReadOnlySpan<byte> payload, out AvlDataPacket? packet)
    {
        packet = null;
        if (payload.Length < 2)
        {
            return false;
        }

        var codec = payload[0];
        if (codec != (byte)CodecId)
        {
            return false;
        }

        var recordCount = payload[1];
        var records = new List<AvlRecord>(recordCount);

        var offset = 2;
        for (var i = 0; i < recordCount; i++)
        {
            if (offset + 24 > payload.Length)
            {
                return false;
            }

            var timestampMs = BinaryPrimitives.ReadInt64BigEndian(payload.Slice(offset, 8));
            offset += 8;
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);

            var priority = payload[offset++];

            var lon = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset, 4)) / 10000000.0;
            offset += 4;
            var lat = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(offset, 4)) / 10000000.0;
            offset += 4;
            var alt = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(offset, 2));
            offset += 2;
            var angle = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(offset, 2));
            offset += 2;
            var sats = payload[offset++];
            var speed = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(offset, 2));
            offset += 2;

            var gps = new GpsElement
            {
                Longitude = lon,
                Latitude = lat,
                Altitude = alt,
                Angle = angle,
                Satellites = sats,
                Speed = speed
            };

            if (offset + 2 > payload.Length)
            {
                return false;
            }

            var eventIoId = payload[offset++];
            var totalIoCount = payload[offset++];

            var properties = new Dictionary<int, byte[]>(totalIoCount);

            // Read 1-byte
            if (offset + 1 > payload.Length)
            {
                return false;
            }
            var n1 = payload[offset++];
            for (var j = 0; j < n1; j++)
            {
                if (offset + 2 > payload.Length)
                {
                    return false;
                }
                properties[payload[offset]] = payload.Slice(offset + 1, 1).ToArray();
                offset += 2;
            }

            // Read 2-byte
            if (offset + 1 > payload.Length)
            {
                return false;
            }
            var n2 = payload[offset++];
            for (var j = 0; j < n2; j++)
            {
                if (offset + 3 > payload.Length)
                {
                    return false;
                }
                properties[payload[offset]] = payload.Slice(offset + 1, 2).ToArray();
                offset += 3;
            }

            // Read 4-byte
            if (offset + 1 > payload.Length)
            {
                return false;
            }
            var n4 = payload[offset++];
            for (var j = 0; j < n4; j++)
            {
                if (offset + 5 > payload.Length)
                {
                    return false;
                }
                properties[payload[offset]] = payload.Slice(offset + 1, 4).ToArray();
                offset += 5;
            }

            // Read 8-byte
            if (offset + 1 > payload.Length)
            {
                return false;
            }
            var n8 = payload[offset++];
            for (var j = 0; j < n8; j++)
            {
                if (offset + 9 > payload.Length)
                {
                    return false;
                }
                properties[payload[offset]] = payload.Slice(offset + 1, 8).ToArray();
                offset += 9;
            }

            var io = new RawIoElement
            {
                EventId = eventIoId,
                TotalIoCount = totalIoCount,
                Properties = properties
            };

            var record = new AvlRecord
            {
                Timestamp = timestamp,
                Priority = priority,
                Gps = gps,
                Io = io
            };

            records.Add(record);
        }

        if (offset >= payload.Length || payload[offset] != recordCount)
        {
            return false;
        }

        packet = new AvlDataPacket
        {
            CodecId = CodecId,
            RecordCount = recordCount,
            Records = records
        };

        return true;
    }
}