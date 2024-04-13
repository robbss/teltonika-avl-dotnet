using System.Buffers.Binary;
using Teltonika.AVL.Attributes;

namespace Teltonika.AVL.Elements.Parsers;

[AvlElementParser(DeviceType.FM2200)]
public class FM2200Parser : IAvlElementParser
{
    public AvlElement[] ParseElements(AvlIOElement[] elements)
    {
        var parsed = new AvlElement[elements.Length];

        for (int i = 0; i < elements.Length; i++)
        {
            parsed[i] = ParseElement(elements[i]);
        }

        return parsed;
    }

    private static AvlElement ParseElement(AvlIOElement element)
    {
        return element.Id switch
        {
            1 or 2 or 3 or 5 or 11 or 12 or 13 or 17 => new AvlElement<byte>(element.Id, element.Value.First()),
            4 or 7 => new AvlElement<double>(element.Id, BinaryPrimitives.ReadInt16BigEndian(element.Value) * 0.001d),
            9 => new AvlElement<double>(element.Id, BinaryPrimitives.ReadInt32BigEndian(element.Value) / 10d),
            10 => new AvlElement<long>(element.Id, BinaryPrimitives.ReadInt64BigEndian(element.Value)),
            _ => throw new NotSupportedException(),
        };
    }
}