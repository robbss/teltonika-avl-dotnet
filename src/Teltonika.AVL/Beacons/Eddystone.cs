namespace Teltonika.AVL.Beacons;

public class Eddystone : TeltonikaBeacon
{
    public required byte[] Namespace { get; set; }

    public required byte[] InstanceId { get; set; }
}