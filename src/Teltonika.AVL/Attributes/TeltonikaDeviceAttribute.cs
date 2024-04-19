namespace Teltonika.AVL.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TeltonikaDeviceAttribute(TrackerType device) : Attribute
{
    public TrackerType DeviceType { get; } = device;
}