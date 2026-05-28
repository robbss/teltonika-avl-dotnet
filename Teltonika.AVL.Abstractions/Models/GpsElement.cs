namespace Teltonika.AVL.Models;

public class GpsElement
{
    public double Longitude { get; init; }
    public double Latitude { get; init; }
    public short Altitude { get; init; }
    public short Angle { get; init; }
    public byte Satellites { get; init; }
    public short Speed { get; init; }
}