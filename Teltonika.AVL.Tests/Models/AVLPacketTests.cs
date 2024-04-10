using System.Collections;

namespace Teltonika.AVL.Tests.Models;

public class AVLPacketTests
{
    /*
    [Theory]
    [ClassData(typeof(MessageTestData))]
    public void FromBytes_WithValidBytes_ReturnsPacket(byte[] message, int dataLength, byte codecId, byte numberOfData)
    {
        var result = AvlHeader.FromBytes(message);

        Assert.Equal(0, result.Preamble);
        Assert.Equal(dataLength, result.DataLength);
        Assert.Equal(codecId, result.CodecId);
        Assert.Equal(numberOfData, result.NumberOfData);
    }

    public class MessageTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { Messages.Example1Codec8.ToByteArray(), 54, 8, 1 };
            yield return new object[] { Messages.Example2Codec8.ToByteArray(), 40, 8, 1 };
            yield return new object[] { Messages.Example3Codec8.ToByteArray(), 67, 8, 2 };
            yield return new object[] { Messages.Example1Codec8Extended.ToByteArray(), 74, 142, 1 };
            yield return new object[] { Messages.Example1Codec16.ToByteArray(), 95, 16, 2 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    */
}