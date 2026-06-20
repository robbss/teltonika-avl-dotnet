namespace Teltonika.Avl.Models;

public readonly record struct GpsData(
    double Longitude,
    double Latitude,
    short Altitude,
    ushort Angle,
    byte Satellites,
    ushort Speed);
