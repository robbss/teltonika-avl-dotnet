using System.Buffers;
using System.Runtime.CompilerServices;
using Teltonika.AVL.Attributes;
using Teltonika.AVL.Extensions;

namespace Teltonika.AVL.Codecs;

[AvlParser(AvlCodec.Codec8Extended)]
public class Codec8ExtendedParser : IAvlParser
{
    public AvlRecord[] ReadRecords(ref SequenceReader<byte> reader, int count)
    {
        var records = new AvlRecord[count];

        for (var i = 0; i < count; i++)
        {
            records[i] = new()
            {
                Timestamp = reader.ReadLongBigEndian(),
                Priority = reader.ReadByte(),
                Longitude = reader.ReadIntBigEndian(),
                Latitude = reader.ReadIntBigEndian(),
                Altitude = reader.ReadShortBigEndian(),
                Angle = reader.ReadShortBigEndian(),
                Satellites = reader.ReadByte(),
                Speed = reader.ReadShortBigEndian(),
                EventId = reader.ReadShortBigEndian(),
                Elements = ReadElements(ref reader)
            };
        }

        return records;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static AvlElement[] ReadElements(ref SequenceReader<byte> reader)
    {
        var elements = new AvlElement[reader.ReadShortBigEndian()];
        var i = 0;

        for (var size = 1; size <= 8; size *= 2)
        {
            var count = reader.ReadShortBigEndian();

            for (; count > 0; --count)
            {
                elements[i++] = new()
                {
                    Id = reader.ReadShortBigEndian(),
                    Value = reader.ReadByteArray(size)
                };
            }
        }

        reader.Advance(2);

        for (; i < elements.Length; i++)
        {
            elements[i] = new()
            {
                Id = reader.ReadShortBigEndian(),
                Value = reader.ReadByteArray(reader.ReadShortBigEndian())
            };
        }

        return elements;
    }
}