namespace Teltonika.AVL.Elements.Devices.Beacons;

public class IBeacon : TeltonikaBeacon
{
    public required byte[] UUID { get; set; }

    public short Major { get; set; }

    public short Minor { get; set; }
}