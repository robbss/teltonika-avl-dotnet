using Teltonika.AVL.Attributes;

namespace Teltonika.AVL.Trackers;

[TeltonikaDevice(TrackerType.FM2200)]
public class FM2200 : TeltonikaTracker
{
    public FM2200() : base()
    {
        Register<ushort>(70, "PCB Temperature", 0.1d, "C");
    }
}