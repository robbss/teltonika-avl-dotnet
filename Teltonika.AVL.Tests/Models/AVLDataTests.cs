using System.Collections;

namespace Teltonika.AVL.Tests.Models;

public class AVLDataTests
{
    /*
    [Theory]
    [ClassData(typeof(MessageTestData))]
    public void FromBytes_WithValidBytes_ReturnsHeader(byte[] message, Codec codec, long timestamp, byte priority, int latitude, int longitude, short altitude, short angle, byte satellites, short speed, short eventId, byte generationType)
    {
        var result = AVLData.FromBytes(message, codec);

        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(priority, result.Priority);
        Assert.Equal(latitude, result.Latitude);
        Assert.Equal(longitude, result.Longitude);
        Assert.Equal(altitude, result.Altitude);
        Assert.Equal(angle, result.Angle);
        Assert.Equal(satellites, result.Satellites);
        Assert.Equal(speed, result.Speed);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(generationType, result.GenerationType);
    }

    public class MessageTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { Messages.Example1Codec8.ToByteArray()[10..], Codec.Codec8, 1560161086000, 1, 0, 0, 0, 0, 0, 0, 1, 0 };
            yield return new object[] { Messages.Example2Codec8.ToByteArray()[10..], Codec.Codec8, 1560161136000, 1, 0, 0, 0, 0, 0, 0, 1, 0 };
            yield return new object[] { Messages.Example3Codec8.ToByteArray()[10..], Codec.Codec8, 1560160861000, 1, 0, 0, 0, 0, 0, 0, 1, 0 };
            yield return new object[] { Messages.Example1Codec8Extended.ToByteArray()[10..], Codec.Codec8Extended, 1560166592000, 1, 0, 0, 0, 0, 0, 0, 1, 0 };
            yield return new object[] { Messages.Example1Codec16.ToByteArray()[10..], Codec.Codec16, 1562760414000, 1, 0, 0, 0, 0, 0, 0, 11, 5 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    */
}