using System.Buffers.Binary;
using System.Collections.Frozen;
using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Elements.Decoding;

public static class ValueDecoder
{
    public static object DecodeValue(
        ReadOnlyMemory<byte> raw,
        IoDataType dataType,
        double multiplier,
        FrozenDictionary<int, string>? enumValues)
    {
        var span = raw.Span;

        return dataType switch
        {
            IoDataType.Boolean => span[0] != 0,
            IoDataType.Unsigned => DecodeUnsigned(span, multiplier, enumValues),
            IoDataType.Signed => DecodeSigned(span, multiplier),
            IoDataType.Hex => FormatHex(span),
            IoDataType.Ascii => System.Text.Encoding.UTF8.GetString(span),
            _ => throw new ArgumentOutOfRangeException(nameof(dataType))
        };
    }

    private static object DecodeUnsigned(ReadOnlySpan<byte> span, double multiplier, FrozenDictionary<int, string>? enumValues)
    {
        ulong value = span.Length switch
        {
            1 => span[0],
            2 => BinaryPrimitives.ReadUInt16BigEndian(span),
            4 => BinaryPrimitives.ReadUInt32BigEndian(span),
            8 => BinaryPrimitives.ReadUInt64BigEndian(span),
            _ => ReadArbitraryUnsigned(span)
        };

        if (enumValues is not null && value <= int.MaxValue && enumValues.TryGetValue((int)value, out var label))
            return label;

        if (multiplier != 1.0)
            return value * multiplier;

        if (span.Length == 8)
            return value;

        return (long)value;
    }

    private static object DecodeSigned(ReadOnlySpan<byte> span, double multiplier)
    {
        long value = span.Length switch
        {
            1 => (sbyte)span[0],
            2 => BinaryPrimitives.ReadInt16BigEndian(span),
            4 => BinaryPrimitives.ReadInt32BigEndian(span),
            8 => BinaryPrimitives.ReadInt64BigEndian(span),
            _ => ReadArbitrarySigned(span)
        };

        if (multiplier != 1.0)
            return value * multiplier;

        return value;
    }

    private static string FormatHex(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
            return string.Empty;

        return string.Join(':', span.ToArray().Select(b => b.ToString("X2")));
    }

    private static ulong ReadArbitraryUnsigned(ReadOnlySpan<byte> span)
    {
        ulong result = 0;
        for (int i = 0; i < span.Length; i++)
            result = (result << 8) | span[i];
        return result;
    }

    private static long ReadArbitrarySigned(ReadOnlySpan<byte> span)
    {
        long result = (span[0] & 0x80) != 0 ? -1L : 0L;
        for (int i = 0; i < span.Length; i++)
            result = (result << 8) | span[i];
        return result;
    }
}
