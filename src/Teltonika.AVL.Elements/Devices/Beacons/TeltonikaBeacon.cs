namespace Teltonika.AVL.Elements.Devices.Beacons;

public class TeltonikaBeacon
{
    public short RSSI { get; set; }

    public short? BatteryVoltage { get; set; }

    public short? Temperature { get; set; }
}