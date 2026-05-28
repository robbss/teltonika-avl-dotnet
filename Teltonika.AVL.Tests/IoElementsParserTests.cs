using Teltonika.AVL.Models;
using Teltonika.AVL.Profiles;
using Teltonika.AVL.Protocol;

namespace Teltonika.AVL.Tests;

public class IoElementsParserTests
{
    [Fact]
    public void Parse_GenericProfile_CorrectlyTranslatesValuesAndMultipliers()
    {
        var parser = new IoElementsParser();
        var profile = new GenericProfile();

        var rawIo = new RawIoElement
        {
            EventId = 239,
            TotalIoCount = 3,
            Properties = new Dictionary<int, byte[]>
            {
                { 239, new byte[] { 1 } }, // Ignition
                { 66, new byte[] { 0x30, 0x39 } }, // External Voltage (12345) -> 12.345V
                { 999, new byte[] { 0x01, 0x02 } } // Unknown
            }
        };

        var parsed = parser.Parse(rawIo, profile);

        Assert.Equal(3, parsed.Count);

        var ignition = parsed[0];
        Assert.Equal(IoType.Ignition, ignition.Type);
        Assert.Equal(IoUnit.None, ignition.Unit);
        Assert.True((bool)ignition.Value);

        var voltage = parsed[1];
        Assert.Equal(IoType.ExternalVoltage, voltage.Type);
        Assert.Equal(IoUnit.Volts, voltage.Unit);
        Assert.Equal(12.345, (double)voltage.Value, 3);

        var unknown = parsed[2];
        Assert.Equal(IoType.Unknown, unknown.Type);
        Assert.Equal(IoUnit.None, unknown.Unit);
        Assert.Equal(new byte[] { 0x01, 0x02 }, (byte[])unknown.Value);
    }
}