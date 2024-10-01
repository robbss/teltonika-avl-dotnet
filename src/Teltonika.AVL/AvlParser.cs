using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using Teltonika.AVL.Attributes;
using Teltonika.AVL.Extensions;

namespace Teltonika.AVL;

public class AvlParser
{
    public AvlParser()
    {
        CodecParsers = [];
        RegisterParsersFromAssembly(Assembly.GetExecutingAssembly());
    }

    public Dictionary<byte, IAvlCodec> CodecParsers { get; protected init; }

    /// <summary>
    /// Reads a <see cref="AvlMessage"/> from the supplied buffer.
    /// </summary>
    public AvlMessage ParseMessage(ref ReadOnlyMemory<byte> buffer, bool verify = true)
    {
        var memory = new ReadOnlySequence<byte>(buffer); 
        return ParseMessage(ref memory, verify);
    }

    public AvlMessage ParseMessage(ref ReadOnlySequence<byte> buffer, bool verify = true)
    {
        var reader = new SequenceReader<byte>(buffer);
        var header = ReadHeader(ref reader);

        if (!CodecParsers.TryGetValue(header.CodecId, out var parser))
        {
            throw new Exception($"No parser registered for codec id {header.CodecId:X2}");
        }

        var message = new AvlMessage()
        {
            Codec = (AvlCodec)header.CodecId,
            Records = parser.Parse(ref reader, header.NumberOfData)
        };

        if (verify)
        {
            if (header.NumberOfData != message.Records.Length || header.NumberOfData != reader.ReadByte())
            {
                throw new NumberOfDataMismatchException();
            }

            if (!VerifyChecksum(ref reader))
            {
                throw new ChecksumMismatchException();
            }
        }

        return message;
    }

    public List<AvlElement> ParseElements(IEnumerable<AvlIOElement> elements, TrackerType trackerType)
    {
        return ParseElements(elements, TeltonikaTrackers.FromType(trackerType));
    }

    public List<AvlElement> ParseElements(IEnumerable<AvlIOElement> elements, TeltonikaTracker tracker)
    {
        var results = new List<AvlElement>();

        foreach (var element in elements)
        {
            if (!tracker.Elements.TryGetValue(element.Id, out var elementInfo))
            {
                throw new NotSupportedException($"No definition for element with id {element.Id} registered");
            }

            results.Add(AvlElementParser.Parse(element.Id, element.Value, elementInfo));
        }

        return results;
    }

    /// <summary>
    /// Register all parsers decorated with <see cref="AvlCodecParserAttribute"/> from the supplied assembly.
    /// </summary>
    public void RegisterParsersFromAssembly(Assembly asm)
    {
        foreach (var type in asm.GetExportedTypes())
        {
            var attr = type.GetCustomAttribute<AvlCodecParserAttribute>();

            if (attr is not null)
            {
                CodecParsers[attr.CodecId] = ((IAvlCodec?)Activator.CreateInstance(type)) ?? throw new Exception($"Unable to create instance of type {type.Name}");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static AvlHeader ReadHeader(ref SequenceReader<byte> reader)
    {
        return new()
        {
            Preamble = reader.ReadIntBigEndian(),
            DataLength = reader.ReadIntBigEndian(),
            CodecId = reader.ReadByte(),
            NumberOfData = reader.ReadByte()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VerifyChecksum(ref SequenceReader<byte> reader)
    {
        var consumed = reader.Consumed - 8;

        reader.Rewind(consumed);

        var crc = 0;

        while (consumed-- > 0)
        {
            crc ^= reader.ReadByte();

            for (int i = 0; i < 8; i++)
            {
                var carry = crc & 1;

                crc >>= 1;

                if (carry == 1)
                {
                    crc ^= 0xA001;
                }
            }
        }

        return crc == reader.ReadIntBigEndian();
    }
}