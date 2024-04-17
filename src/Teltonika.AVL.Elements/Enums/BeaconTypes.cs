namespace Teltonika.AVL.Elements;

[Flags]
public enum BeaconTypes : byte
{
    iBeacon = 0x21,
    Eddystone = 0x01,

    Battery = 0x02,
    Temperature = 0x04
}