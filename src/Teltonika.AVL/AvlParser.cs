using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using Teltonika.AVL.Attributes;
using Teltonika.AVL.Extensions;
using Teltonika.AVL.Models;

namespace Teltonika.AVL;

public class AvlParser
{
    public AvlParser()
    {
        CodecParsers = [];
        RegisterParsersFromAssembly(Assembly.GetExecutingAssembly());
    }

    public Dictionary<byte, IAvlParser> CodecParsers { get; protected init; }

    /// <summary>
    /// Reads a <see cref="AvlMessage"/> from the supplied buffer.
    /// </summary>
    public AvlMessage ReadMessage(ref ReadOnlyMemory<byte> buffer, bool verify = true)
    {
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));
        var header = ReadHeader(ref reader);

        if (!CodecParsers.TryGetValue(header.CodecId, out var parser))
        {
            throw new Exception($"No parser registered for codec id {header.CodecId:X2}");
        }

        var message = new AvlMessage()
        {
            Codec = (AvlCodec)header.CodecId,
            Records = parser.ReadRecords(ref reader, header.NumberOfData)
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

    /// <summary>
    /// Register all parsers decorated with <see cref="AvlParserAttribute"/> from the supplied assembly.
    /// </summary>
    public void RegisterParsersFromAssembly(Assembly asm)
    {
        foreach (var type in asm.GetExportedTypes())
        {
            var attr = type.GetCustomAttribute<AvlParserAttribute>();

            if (attr is not null)
            {
                CodecParsers[attr.CodecId] = ((IAvlParser?)Activator.CreateInstance(type)) ?? throw new Exception($"Unable to create instance of type {type.Name}");
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