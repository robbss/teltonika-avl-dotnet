namespace Teltonika.AVL;

public class TeltonikaBeacon
{
    public sbyte TxPower { get; set; }

    public short? BatteryVoltage { get; set; }

    public short? Temperature { get; set; }
}