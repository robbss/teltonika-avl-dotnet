using Teltonika.AVL.Elements;

namespace Teltonika.AVL.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TeltonikaDeviceAttribute(DeviceType device) : Attribute
{
    public DeviceType DeviceType { get; } = device;
}