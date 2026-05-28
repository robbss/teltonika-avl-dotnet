namespace Teltonika.AVL.Models;

public enum IoType
{
    Unknown = 0,
    Ignition = 239,
    Movement = 240,
    ExternalVoltage = 66,
    BatteryVoltage = 67,
    BatteryCurrent = 68,
    GnssStatus = 69,
    Speed = 24,
    GsmSignal = 21,
    SleepMode = 200,
    Odometer = 16,
    DigitalInput1 = 1,
    DigitalInput2 = 2,
    DigitalOutput1 = 179,
    DigitalOutput2 = 180,
    AnalogInput1 = 9,
    DataMode = 80,
    TripOdometer = 199,
    AxisX = 17,
    AxisY = 18,
    AxisZ = 19,
    GreenDrivingType = 253,
    GreenDrivingValue = 254,
    EcoScore = 31,
    CrashDetection = 247,
    UnplugDetection = 252,
    TotalOdometer = 87
}