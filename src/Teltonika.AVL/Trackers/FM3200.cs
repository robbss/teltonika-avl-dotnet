using Teltonika.AVL.Attributes;

namespace Teltonika.AVL.Trackers;

[TeltonikaDevice(TrackerType.FM3200)]
public class FM3200 : TeltonikaTracker
{
    public FM3200() : base()
    {
        Register<byte>(69, "GPS Power");
        Register<ushort>(70, "PCB Temperature", 0.1d, "C");
    }
}