namespace Teltonika.AVL;

public class AvlRecord
{
    public long Timestamp { get; set; }

    public byte Priority { get; set; }

    public int Longitude { get; set; }

    public int Latitude { get; set; }

    public short Altitude { get; set; }

    public short Angle { get; set; }

    public byte Satellites { get; set; }

    public short Speed { get; set; }

    public short EventId { get; set; }

    public byte? GenerationType { get; set; }

    public AvlElement[] Elements { get; set; } = [];
}