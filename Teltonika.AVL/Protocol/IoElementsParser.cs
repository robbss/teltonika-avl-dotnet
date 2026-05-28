using System.Buffers.Binary;
using Teltonika.AVL.Models;
using Teltonika.AVL.Profiles;

namespace Teltonika.AVL.Protocol;

public class IoElementsParser : IIoElementsParser
{
    public IReadOnlyList<IoElement> Parse(RawIoElement rawIo, IIoProfile profile)
    {
        var result = new List<IoElement>(rawIo.Properties.Count);

        foreach (var kvp in rawIo.Properties)
        {
            var rawId = kvp.Key;
            var data = kvp.Value;

            if (profile.TryGetDefinition(rawId, out var def) && def != null)
            {
                var value = ParseValue(data, def.DataType);

                if (def.Multiplier != 1.0)
                {
                    value = ApplyMultiplier(value, def.Multiplier);
                }

                result.Add(new IoElement
                {
                    RawId = rawId,
                    Type = def.Type,
                    Value = value,
                    Unit = def.Unit
                });
            }
            else
            {
                result.Add(new IoElement
                {
                    RawId = rawId,
                    Type = IoType.Unknown,
                    Value = data,
                    Unit = IoUnit.None
                });
            }
        }

        return result;
    }

    private static object ParseValue(byte[] data, IoDataType dataType)
    {
        return dataType switch
        {
            IoDataType.Boolean => data.Length > 0 && data[0] > 0,
            IoDataType.SignedInt => data.Length switch
            {
                1 => (sbyte)data[0],
                2 => BinaryPrimitives.ReadInt16BigEndian(data),
                4 => BinaryPrimitives.ReadInt32BigEndian(data),
                8 => BinaryPrimitives.ReadInt64BigEndian(data),
                _ => data
            },
            IoDataType.UnsignedInt => data.Length switch
            {
                1 => data[0],
                2 => BinaryPrimitives.ReadUInt16BigEndian(data),
                4 => BinaryPrimitives.ReadUInt32BigEndian(data),
                8 => BinaryPrimitives.ReadUInt64BigEndian(data),
                _ => data
            },
            _ => data
        };
    }

    private static object ApplyMultiplier(object value, double multiplier)
    {
        return value switch
        {
            sbyte v => v * multiplier,
            short v => v * multiplier,
            int v => v * multiplier,
            long v => v * multiplier,
            byte v => v * multiplier,
            ushort v => v * multiplier,
            uint v => v * multiplier,
            ulong v => v * multiplier,
            _ => value
        };
    }
}