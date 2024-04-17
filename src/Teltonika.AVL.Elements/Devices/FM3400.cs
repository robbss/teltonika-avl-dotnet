using Teltonika.AVL.Attributes;
using Teltonika.AVL.Elements.Parsers;

namespace Teltonika.AVL.Elements.Devices;

[TeltonikaDevice(DeviceType.FM3400)]
public class FM3400 : TeltonikaDevice
{
    public FM3400() : base()
    {
        Register<ushort>(69, "GPS/GNSS Power", default, default);
        Register<uint>(70, "PCB Temperature", default, default);
        Register<byte>(71, "Jamming Detection", default, default);
    }
}