using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Elements.Tests;

public class FmbElementNamingTests
{
    [Theory]
    [InlineData((ushort)9, "Analog Input 1")]
    [InlineData((ushort)6, "Analog Input 2")]
    [InlineData((ushort)179, "Digital Output 1")]
    [InlineData((ushort)180, "Digital Output 2")]
    [InlineData((ushort)200, "Sleep Mode")]
    public void FmbFamilyIds_CarryTheirFmbNames(ushort id, string name)
    {
        var definition = IoElementResolver.GetDefinition(id, TrackerModel.FMC234);

        Assert.NotNull(definition);
        Assert.Equal(name, definition!.Name);
    }

    [Theory]
    [InlineData(0, "Off")]
    [InlineData(1, "On, Fix")]
    [InlineData(2, "On, No Fix")]
    [InlineData(3, "Sleep")]
    public void GnssStatus_UsesFmbSemantics(int raw, string label)
    {
        var property = IoElementResolver.Encode(69, raw, TrackerModel.FMC234);
        var resolved = IoElementResolver.Resolve(property, TrackerModel.FMC234);

        Assert.Equal(label, resolved!.Value.Value);
    }

    [Theory]
    [InlineData(0, "No Sleep")]
    [InlineData(1, "GPS Sleep")]
    [InlineData(2, "Deep Sleep")]
    [InlineData(3, "Online Deep Sleep")]
    [InlineData(4, "Ultra Deep Sleep")]
    public void SleepMode_CoversAllFiveStates(int raw, string label)
    {
        var property = IoElementResolver.Encode(200, raw, TrackerModel.FMB204);
        var resolved = IoElementResolver.Resolve(property, TrackerModel.FMB204);

        Assert.Equal(label, resolved!.Value.Value);
    }

    [Fact]
    public void Iccid_IsEightBytesWide()
    {
        Assert.Equal(8, IoElementResolver.GetDefinition(11)!.ValueSize);
        Assert.Equal(8, IoElementResolver.Encode(11, 8901234567890123ul).Value.Length);
    }
}
