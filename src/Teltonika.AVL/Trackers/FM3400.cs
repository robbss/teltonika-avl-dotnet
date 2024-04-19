using Teltonika.AVL.Attributes;

namespace Teltonika.AVL.Trackers;

[TeltonikaDevice(TrackerType.FM3400)]
public class FM3400 : TeltonikaTracker
{
    public FM3400() : base()
    {
        Register<ushort>(69, "GPS/GNSS Power", default, default);
        Register<uint>(70, "PCB Temperature", default, default);
        Register<byte>(71, "Jamming Detection", default, default);
    }
}