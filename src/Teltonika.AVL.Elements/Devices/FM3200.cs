using Teltonika.AVL.Attributes;
using Teltonika.AVL.Elements.Parsers;

namespace Teltonika.AVL.Elements.Devices;

[TeltonikaDevice(DeviceType.FM3200)]
public class FM3200 : TeltonikaDevice
{
    public FM3200() : base()
    {
        Register<byte>(69, "GPS Power");
        Register<ushort>(70, "PCB Temperature", 0.1d, "C");
    }
}