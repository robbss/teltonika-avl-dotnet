using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Teltonika.AVL.Tests;

public class AvlReaderTests
{
    /*
    private readonly AvlReader reader;

    public AvlReaderTests()
    {
        reader = new();
    }

    [Theory]
    [ClassData(typeof(MessageTestData))]
    public void Read_Codec8WithValidBytes_ReturnsAVLMessage(byte[] message, Codec codec, AvlRecord expected)
    {
        var actual = AvlReader.Read(message);

        Assert.Equal(expected.Packet.CodecId, actual.Packet.CodecId);
    }

    public class MessageTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { Messages.Example1Codec8.ToByteArray(), Codec.Codec8, new AvlRecord() { } };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    */
}