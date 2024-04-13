using Teltonika.AVL.Elements;

namespace Teltonika.AVL.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AvlElementParserAttribute(DeviceType device) : Attribute
{
    public DeviceType Device { get; } = device;
}