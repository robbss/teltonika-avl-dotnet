using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using Teltonika.AVL.Attributes;
using Teltonika.AVL.Extensions;
using Teltonika.AVL.Models;

namespace Teltonika.AVL;

public class AvlReader
{
    public AvlReader()
    {
        Parsers = [];
        RegisterParsersFromAssembly(Assembly.GetExecutingAssembly());
    }

    public Dictionary<byte, IAvlParser> Parsers { get; protected init; }

    /// <summary>
    /// Reads a <see cref="AvlMessage"/> from the supplied buffer.
    /// </summary>
    public AvlMessage ReadMessage(ref ReadOnlyMemory<byte> buffer)
    {
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));
        var header = ReadHeader(ref reader);

        if (!Parsers.TryGetValue(header.CodecId, out var parser))
        {
            throw new Exception($"No parser registered for codec id {header.CodecId:X2}");
        }

        return new()
        {
            Header = header,
            Records = parser.ReadRecords(ref reader, header.NumberOfData)
        };
    }

    /// <summary>
    /// Register all parsers decorated with <see cref="AvlParserAttribute"/> in the supplied assembly.
    /// </summary>
    public void RegisterParsersFromAssembly(Assembly asm)
    {
        foreach (var type in asm.GetExportedTypes())
        {
            var attr = type.GetCustomAttribute<AvlParserAttribute>();

            if (attr is not null)
            {
                Parsers[attr.CodecId] = ((IAvlParser?)Activator.CreateInstance(type)) ?? throw new Exception($"Unable to create instance of type {type.Name}");
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
}