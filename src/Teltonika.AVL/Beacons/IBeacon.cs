namespace Teltonika.AVL.Beacons;

public class IBeacon : TeltonikaBeacon
{
    public required Guid Uuid { get; set; }

    public ushort Major { get; set; }

    public ushort Minor { get; set; }
}