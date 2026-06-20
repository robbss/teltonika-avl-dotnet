using Teltonika.Avl.Elements.Models;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Elements.Tests;

public class IoElementResolverTests
{
    [Fact]
    public void Resolve_ExternalVoltage_Unsigned2Bytes()
    {
        var prop = new IoProperty(66, new byte[] { 0x2E, 0xE0 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal(66, result.Value.Id);
        Assert.Equal("External Voltage", result.Value.Name);
        Assert.Equal(12000L, result.Value.Value);
        Assert.Equal("mV", result.Value.Units);
        Assert.Equal("Permanent", result.Value.Group);
    }

    [Fact]
    public void Resolve_Ignition_Boolean()
    {
        var prop = new IoProperty(239, new byte[] { 0x01 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("Ignition", result.Value.Name);
        Assert.IsType<bool>(result.Value.Value);
        Assert.True((bool)result.Value.Value);
    }

    [Fact]
    public void Resolve_GnssPdop_WithMultiplier()
    {
        var prop = new IoProperty(181, new byte[] { 0x00, 0x0F });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("GNSS PDOP", result.Value.Name);
        Assert.IsType<double>(result.Value.Value);
        Assert.Equal(1.5, (double)result.Value.Value, 5);
    }

    [Fact]
    public void Resolve_GreenDrivingType_Enum()
    {
        var prop = new IoProperty(253, new byte[] { 0x02 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("Green Driving Type", result.Value.Name);
        Assert.Equal("Harsh Braking", result.Value.Value);
    }

    [Fact]
    public void Resolve_UnknownId_ReturnsNull()
    {
        var prop = new IoProperty(9999, new byte[] { 0x01 });
        var result = IoElementResolver.Resolve(prop);

        Assert.Null(result);
    }

    [Fact]
    public void TryResolve_KnownId_ReturnsTrue()
    {
        var prop = new IoProperty(240, new byte[] { 0x01 });
        bool success = IoElementResolver.TryResolve(prop, out var resolved);

        Assert.True(success);
        Assert.Equal("Movement", resolved.Name);
        Assert.True((bool)resolved.Value);
    }

    [Fact]
    public void TryResolve_UnknownId_ReturnsFalse()
    {
        var prop = new IoProperty(9999, new byte[] { 0x01 });
        bool success = IoElementResolver.TryResolve(prop, out _);

        Assert.False(success);
    }

    [Fact]
    public void ResolveAll_MixedKnownAndUnknown()
    {
        var element = new IoElement(0, new IoProperty[]
        {
            new(239, new byte[] { 0x01 }),     // known: Ignition
            new(9999, new byte[] { 0xFF }),     // unknown
            new(240, new byte[] { 0x00 }),      // known: Movement
            new(21, new byte[] { 0x03 }),       // known: GSM Signal
        });

        var results = IoElementResolver.ResolveAll(element);

        Assert.Equal(3, results.Count);
        Assert.Equal("Ignition", results[0].Name);
        Assert.Equal("Movement", results[1].Name);
        Assert.Equal("GSM Signal", results[2].Name);
    }

    [Fact]
    public void GetDefinition_ReturnsDefinition()
    {
        var def = IoElementResolver.GetDefinition(66);

        Assert.NotNull(def);
        Assert.Equal("External Voltage", def.Name);
        Assert.Equal(IoDataType.Unsigned, def.DataType);
        Assert.Equal((byte)2, def.ValueSize);
        Assert.Equal("mV", def.Units);
    }

    [Fact]
    public void GetDefinition_UnknownId_ReturnsNull()
    {
        Assert.Null(IoElementResolver.GetDefinition(9999));
    }

    [Fact]
    public void IsSupported_KnownId_ReturnsTrue()
    {
        Assert.True(IoElementResolver.IsSupported(66, TrackerModel.FMB920));
    }

    [Fact]
    public void IsSupported_UnknownId_ReturnsFalse()
    {
        Assert.False(IoElementResolver.IsSupported(9999, TrackerModel.FMB920));
    }

    [Fact]
    public void Resolve_SignedTemperature_WithMultiplier()
    {
        // Dallas Temperature: -12.5°C = -125 raw, 4-byte big-endian signed: 0xFFFFFF83
        var prop = new IoProperty(72, new byte[] { 0xFF, 0xFF, 0xFF, 0x83 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("Dallas Temperature 1", result.Value.Name);
        Assert.IsType<double>(result.Value.Value);
        Assert.Equal(-12.5, (double)result.Value.Value, 1);
    }

    [Fact]
    public void Resolve_IButton_Hex()
    {
        var prop = new IoProperty(78, new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("iButton", result.Value.Name);
        Assert.Equal("01:23:45:67:89:AB:CD:EF", result.Value.Value);
    }

    [Fact]
    public void Resolve_Vin_Ascii()
    {
        var vin = "WVWZZZ3CZWE123456"u8.ToArray();
        var prop = new IoProperty(256, vin);
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("VIN", result.Value.Name);
        Assert.Equal("WVWZZZ3CZWE123456", result.Value.Value);
    }

    [Fact]
    public void Resolve_DataMode_Enum()
    {
        var prop = new IoProperty(80, new byte[] { 0x03 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("Data Mode", result.Value.Name);
        Assert.Equal("Roaming On Moving", result.Value.Value);
    }

    // --- Per-model override tests ---

    [Fact]
    public void Resolve_WithModel_ReturnsOverride()
    {
        var prop = new IoProperty(389, new byte[] { 18 });
        var result = IoElementResolver.Resolve(prop, TrackerModel.TMT250);

        Assert.NotNull(result);
        Assert.Equal("Button Click", result.Value.Name);
        Assert.Equal("Alarm Button Single Click", result.Value.Value);
    }

    [Fact]
    public void Resolve_WithModel_NoOverride_FallsBackToBase()
    {
        var prop = new IoProperty(389, new byte[] { 0x04 });
        var result = IoElementResolver.Resolve(prop, TrackerModel.FMB920);

        Assert.NotNull(result);
        Assert.Equal("OBD Fuel Type", result.Value.Name);
        Assert.Equal("Diesel", result.Value.Value);
    }

    [Fact]
    public void Resolve_WithoutModel_ReturnsBase()
    {
        var prop = new IoProperty(389, new byte[] { 0x01 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("OBD Fuel Type", result.Value.Name);
        Assert.Equal("Gasoline", result.Value.Value);
    }

    [Fact]
    public void GetDefinition_WithModel_ReturnsOverride()
    {
        var def = IoElementResolver.GetDefinition(389, TrackerModel.TMT250);

        Assert.NotNull(def);
        Assert.Equal("Button Click", def.Name);
        Assert.Equal("Eventual", def.Group);
    }

    [Fact]
    public void GetDefinition_WithModel_NoOverride_ReturnsBase()
    {
        var def = IoElementResolver.GetDefinition(389, TrackerModel.FMB920);

        Assert.NotNull(def);
        Assert.Equal("OBD Fuel Type", def.Name);
    }

    [Fact]
    public void ResolveAll_WithModel_UsesOverrides()
    {
        var element = new IoElement(0, new IoProperty[]
        {
            new(239, new byte[] { 0x01 }),     // Ignition (no override)
            new(389, new byte[] { 33 }),        // Power Button Single Click on TMT250
        });

        var results = IoElementResolver.ResolveAll(element, TrackerModel.TMT250);

        Assert.Equal(2, results.Count);
        Assert.Equal("Ignition", results[0].Name);
        Assert.Equal("Button Click", results[1].Name);
        Assert.Equal("Power Button Single Click", results[1].Value);
    }

    [Fact]
    public void Resolve_FM6300_BatteryVoltage_Override()
    {
        // ID 12 = "Fuel Used GPS" on FMB, but "Battery Voltage" on FM6300
        var prop = new IoProperty(12, new byte[] { 0x2E, 0xE0 });
        var result = IoElementResolver.Resolve(prop, TrackerModel.FM6300);

        Assert.NotNull(result);
        Assert.Equal("Battery Voltage", result.Value.Name);
        Assert.Equal("mV", result.Value.Units);
        Assert.Equal(12000L, result.Value.Value);
    }

    [Fact]
    public void Resolve_FM6300_BatteryVoltage_BaseUnchanged()
    {
        var prop = new IoProperty(12, new byte[] { 0x00, 0x00, 0x03, 0xE8 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("Fuel Used GPS", result.Value.Name);
        Assert.Equal("ml", result.Value.Units);
    }

    [Fact]
    public void Resolve_FMC003_Towing_2Byte_Override()
    {
        // FMC003 sends towing as 2 bytes — first byte 0x00, second byte has value
        var prop = new IoProperty(246, new byte[] { 0x00, 0x01 });
        var result = IoElementResolver.Resolve(prop, TrackerModel.FMC003);

        Assert.NotNull(result);
        Assert.Equal("Towing", result.Value.Name);
        Assert.Equal(1L, result.Value.Value);
    }

    [Fact]
    public void Resolve_TamperDetection()
    {
        var prop = new IoProperty(20019, new byte[] { 0x00, 0x02 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("Tamper Detection", result.Value.Name);
        Assert.Equal("Attached to Metal Surface", result.Value.Value);
    }

    [Fact]
    public void Resolve_ManDown_GH5200()
    {
        var prop = new IoProperty(242, new byte[] { 0x01 });
        var result = IoElementResolver.Resolve(prop);

        Assert.NotNull(result);
        Assert.Equal("ManDown/FallDown", result.Value.Name);
        Assert.Equal("Active", result.Value.Value);
    }

    [Fact]
    public void IsSupported_TamperDetection_TAT240()
    {
        Assert.True(IoElementResolver.IsSupported(20019, TrackerModel.TAT240));
    }

    [Fact]
    public void IsSupported_TamperDetection_FMB920_NotSupported()
    {
        Assert.False(IoElementResolver.IsSupported(20019, TrackerModel.FMB920));
    }
}
