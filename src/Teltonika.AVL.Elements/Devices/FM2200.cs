using Teltonika.AVL.Attributes;
using Teltonika.AVL.Elements.Parsers;

namespace Teltonika.AVL.Elements.Devices;

[TeltonikaDevice(DeviceType.FM2200)]
public class FM2200 : TeltonikaDevice
{
    public FM2200() : base()
    {
        Register<ushort>(70, "PCB Temperature", 0.1d, "C");
    }
}