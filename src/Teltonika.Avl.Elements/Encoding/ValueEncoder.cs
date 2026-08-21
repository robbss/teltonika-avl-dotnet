using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Globalization;
using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Elements.Encoding;

/// <summary>
/// Writes a logical IO value back to the raw bytes a device would send,
/// inverting <see cref="Decoding.ValueDecoder"/>.
/// </summary>
public static class ValueEncoder
{
    /// <summary>Encodes <paramref name="value"/> as the raw bytes for an IO element.</summary>
    public static byte[] EncodeValue(
        object value,
        IoDataType dataType,
        byte valueSize,
        double multiplier,
        FrozenDictionary<int, string>? enumValues = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        return dataType switch
        {
            IoDataType.Boolean => [ToBoolean(value) ? (byte)1 : (byte)0],
            IoDataType.Unsigned => EncodeUnsigned(value, valueSize, multiplier, enumValues),
            IoDataType.Signed => EncodeSigned(value, valueSize, multiplier),
            IoDataType.Hex => EncodeHex(value, valueSize),
            IoDataType.Ascii => EncodeAscii(value, valueSize),
            _ => throw new ArgumentOutOfRangeException(nameof(dataType))
        };
    }

    private static bool ToBoolean(object value) =>
        value switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var parsed)
                ? parsed
                : Convert.ToDouble(s, CultureInfo.InvariantCulture) != 0,
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0
        };

    private static byte[] EncodeUnsigned(object value, byte valueSize, double multiplier, FrozenDictionary<int, string>? enumValues)
    {
        var raw = value is string label && enumValues is not null && TryLookupEnum(label, enumValues, out var code)
            ? (ulong)code
            : ToUnsignedRaw(value, multiplier);

        return WriteBigEndian(raw, Width(valueSize, raw));
    }

    private static byte[] EncodeSigned(object value, byte valueSize, double multiplier)
    {
        var raw = ToSignedRaw(value, multiplier);
        return WriteBigEndian(unchecked((ulong)raw), Width(valueSize, raw < 0 ? ulong.MaxValue : (ulong)raw));
    }

    private static ulong ToUnsignedRaw(object value, double multiplier)
    {
        if (multiplier == 1.0)
        {
            switch (value)
            {
                case ulong u: return u;
                case long l when l >= 0: return (ulong)l;
                case int i when i >= 0: return (ulong)i;
            }
        }

        var scaled = Convert.ToDouble(value, CultureInfo.InvariantCulture) / (multiplier == 0 ? 1.0 : multiplier);
        var rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);

        if (rounded is < 0 or > 18446744073709551615.0)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} does not fit an unsigned IO element.");

        return (ulong)rounded;
    }

    private static long ToSignedRaw(object value, double multiplier)
    {
        if (multiplier == 1.0 && value is long or int or short or sbyte)
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);

        var scaled = Convert.ToDouble(value, CultureInfo.InvariantCulture) / (multiplier == 0 ? 1.0 : multiplier);
        var rounded = Math.Round(scaled, MidpointRounding.AwayFromZero);

        if (rounded is < -9223372036854775808.0 or > 9223372036854775807.0)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} does not fit a signed IO element.");

        return (long)rounded;
    }

    private static int Width(byte valueSize, ulong magnitude)
    {
        if (valueSize > 0)
            return valueSize;

        return magnitude switch
        {
            <= byte.MaxValue => 1,
            <= ushort.MaxValue => 2,
            <= uint.MaxValue => 4,
            _ => 8
        };
    }

    private static byte[] WriteBigEndian(ulong raw, int width)
    {
        var bytes = new byte[width];

        switch (width)
        {
            case 1:
                bytes[0] = (byte)raw;
                break;
            case 2:
                BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)raw);
                break;
            case 4:
                BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)raw);
                break;
            case 8:
                BinaryPrimitives.WriteUInt64BigEndian(bytes, raw);
                break;
            default:
                for (var i = width - 1; i >= 0; i--)
                {
                    bytes[i] = (byte)raw;
                    raw >>= 8;
                }
                break;
        }

        return bytes;
    }

    private static byte[] EncodeHex(object value, byte valueSize)
    {
        var bytes = value switch
        {
            byte[] raw => raw,
            ReadOnlyMemory<byte> memory => memory.ToArray(),
            string text => ParseHex(text),
            _ => throw new ArgumentException($"Hex IO values must be a hex string or bytes, got {value.GetType().Name}.", nameof(value))
        };

        return Fit(bytes, valueSize, padLeft: true);
    }

    private static byte[] EncodeAscii(object value, byte valueSize)
    {
        var bytes = value switch
        {
            byte[] raw => raw,
            ReadOnlyMemory<byte> memory => memory.ToArray(),
            _ => System.Text.Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
        };

        return Fit(bytes, valueSize, padLeft: false);
    }

    private static byte[] ParseHex(string text)
    {
        Span<char> digits = stackalloc char[text.Length];
        var length = 0;

        foreach (var c in text)
        {
            if (c is ':' or '-' or ' ' or '_')
                continue;

            digits[length++] = c;
        }

        if (length % 2 != 0)
            throw new ArgumentException($"Hex value '{text}' has an odd number of digits.", nameof(text));

        return Convert.FromHexString(digits[..length]);
    }

    private static byte[] Fit(byte[] bytes, byte valueSize, bool padLeft)
    {
        if (valueSize == 0 || bytes.Length == valueSize)
            return bytes;

        if (bytes.Length > valueSize)
            throw new ArgumentOutOfRangeException(nameof(bytes), $"Value is {bytes.Length} bytes, but the element holds {valueSize}.");

        var fitted = new byte[valueSize];
        bytes.CopyTo(fitted, padLeft ? valueSize - bytes.Length : 0);
        return fitted;
    }

    private static bool TryLookupEnum(string label, FrozenDictionary<int, string> enumValues, out int code)
    {
        foreach (var pair in enumValues)
        {
            if (string.Equals(pair.Value, label, StringComparison.OrdinalIgnoreCase))
            {
                code = pair.Key;
                return true;
            }
        }

        code = 0;
        return false;
    }
}
