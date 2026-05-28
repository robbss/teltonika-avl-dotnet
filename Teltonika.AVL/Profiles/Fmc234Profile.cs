using Teltonika.AVL.Models;

namespace Teltonika.AVL.Profiles;

public class Fmc234Profile : IIoProfile
{
    public string Name => "FMC234";

    private readonly Dictionary<int, IoPropertyDefinition> _definitions = new()
    {
        { 239, new IoPropertyDefinition { Type = IoType.Ignition, DataType = IoDataType.Boolean } },
        { 240, new IoPropertyDefinition { Type = IoType.Movement, DataType = IoDataType.Boolean } },
        { 66, new IoPropertyDefinition { Type = IoType.ExternalVoltage, DataType = IoDataType.UnsignedInt, Multiplier = 0.001, Unit = IoUnit.Volts } },
        { 67, new IoPropertyDefinition { Type = IoType.BatteryVoltage, DataType = IoDataType.UnsignedInt, Multiplier = 0.001, Unit = IoUnit.Volts } },
        { 68, new IoPropertyDefinition { Type = IoType.BatteryCurrent, DataType = IoDataType.UnsignedInt, Multiplier = 0.001, Unit = IoUnit.Amperes } },
        { 69, new IoPropertyDefinition { Type = IoType.GnssStatus, DataType = IoDataType.UnsignedInt } },
        { 24, new IoPropertyDefinition { Type = IoType.Speed, DataType = IoDataType.UnsignedInt, Unit = IoUnit.KilometersPerHour } },
        { 21, new IoPropertyDefinition { Type = IoType.GsmSignal, DataType = IoDataType.UnsignedInt } },
        { 200, new IoPropertyDefinition { Type = IoType.SleepMode, DataType = IoDataType.UnsignedInt } },
        { 16, new IoPropertyDefinition { Type = IoType.Odometer, DataType = IoDataType.UnsignedInt, Unit = IoUnit.Meters } },
        { 1, new IoPropertyDefinition { Type = IoType.DigitalInput1, DataType = IoDataType.Boolean } },
        { 2, new IoPropertyDefinition { Type = IoType.DigitalInput2, DataType = IoDataType.Boolean } },
        { 179, new IoPropertyDefinition { Type = IoType.DigitalOutput1, DataType = IoDataType.Boolean } },
        { 180, new IoPropertyDefinition { Type = IoType.DigitalOutput2, DataType = IoDataType.Boolean } },
        { 9, new IoPropertyDefinition { Type = IoType.AnalogInput1, DataType = IoDataType.UnsignedInt, Multiplier = 0.001, Unit = IoUnit.Volts } },
        { 80, new IoPropertyDefinition { Type = IoType.DataMode, DataType = IoDataType.UnsignedInt } },
        { 199, new IoPropertyDefinition { Type = IoType.TripOdometer, DataType = IoDataType.UnsignedInt, Unit = IoUnit.Meters } },
        { 17, new IoPropertyDefinition { Type = IoType.AxisX, DataType = IoDataType.SignedInt } },
        { 18, new IoPropertyDefinition { Type = IoType.AxisY, DataType = IoDataType.SignedInt } },
        { 19, new IoPropertyDefinition { Type = IoType.AxisZ, DataType = IoDataType.SignedInt } },
        { 253, new IoPropertyDefinition { Type = IoType.GreenDrivingType, DataType = IoDataType.UnsignedInt } },
        { 254, new IoPropertyDefinition { Type = IoType.GreenDrivingValue, DataType = IoDataType.SignedInt } },
        { 31, new IoPropertyDefinition { Type = IoType.EcoScore, DataType = IoDataType.UnsignedInt } },
        { 247, new IoPropertyDefinition { Type = IoType.CrashDetection, DataType = IoDataType.UnsignedInt } },
        { 252, new IoPropertyDefinition { Type = IoType.UnplugDetection, DataType = IoDataType.Boolean } },
        { 87, new IoPropertyDefinition { Type = IoType.TotalOdometer, DataType = IoDataType.UnsignedInt, Unit = IoUnit.Meters } }
    };

    public bool TryGetDefinition(int rawId, out IoPropertyDefinition? definition)
    {
        return _definitions.TryGetValue(rawId, out definition);
    }
}