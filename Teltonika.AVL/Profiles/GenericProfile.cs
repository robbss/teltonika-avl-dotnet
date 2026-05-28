using Teltonika.AVL.Models;

namespace Teltonika.AVL.Profiles;

public class GenericProfile : IIoProfile
{
    public string Name => "Generic";

    private readonly Dictionary<int, IoPropertyDefinition> _definitions = new()
    {
        { 239, new IoPropertyDefinition { Type = IoType.Ignition, DataType = IoDataType.Boolean } },
        { 240, new IoPropertyDefinition { Type = IoType.Movement, DataType = IoDataType.Boolean } },
        { 66, new IoPropertyDefinition { Type = IoType.ExternalVoltage, DataType = IoDataType.UnsignedInt, Multiplier = 0.001, Unit = IoUnit.Volts } },
        { 67, new IoPropertyDefinition { Type = IoType.BatteryVoltage, DataType = IoDataType.UnsignedInt, Multiplier = 0.001, Unit = IoUnit.Volts } },
        { 24, new IoPropertyDefinition { Type = IoType.Speed, DataType = IoDataType.UnsignedInt, Unit = IoUnit.KilometersPerHour } },
        { 21, new IoPropertyDefinition { Type = IoType.GsmSignal, DataType = IoDataType.UnsignedInt } },
        { 16, new IoPropertyDefinition { Type = IoType.Odometer, DataType = IoDataType.UnsignedInt, Unit = IoUnit.Meters } }
    };

    public bool TryGetDefinition(int rawId, out IoPropertyDefinition? definition)
    {
        return _definitions.TryGetValue(rawId, out definition);
    }
}