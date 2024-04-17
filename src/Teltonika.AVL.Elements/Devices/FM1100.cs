using Teltonika.AVL.Attributes;
using Teltonika.AVL.Elements.Parsers;

namespace Teltonika.AVL.Elements.Devices;

[TeltonikaDevice(DeviceType.FM1100)]
public class FM1100 : TeltonikaDevice
{
    public FM1100() : base()
    {
        Register<ushort>(4, "Analog Input 1", default, "mV");
        Register<byte>(5, "GSM level");
        Register<ushort>(6, "Speed", default, "km/h");
        // TODO
        throw new NotImplementedException();
    }
}