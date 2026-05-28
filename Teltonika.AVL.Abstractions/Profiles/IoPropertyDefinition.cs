using Teltonika.AVL.Models;

namespace Teltonika.AVL.Profiles;

public class IoPropertyDefinition
{
    public IoType Type { get; init; }
    public IoDataType DataType { get; init; }
    public double Multiplier { get; init; } = 1.0;
    public IoUnit Unit { get; init; } = IoUnit.None;
}