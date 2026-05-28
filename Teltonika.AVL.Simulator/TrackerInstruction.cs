namespace Teltonika.AVL.Simulator;

public enum InstructionType
{
    SetIgnition,
    Wait,
    MoveTo
}

public class TrackerInstruction
{
    public InstructionType Type { get; set; }
    public bool? IgnitionValue { get; set; }
    public int? DurationSeconds { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Speed { get; set; } // km/h
}
