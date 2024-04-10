using System.Buffers;
using Teltonika.AVL.Codecs;

namespace Teltonika.AVL.Tests.Codecs.Codec8Extended;

public class Codec8ExtendedHandlerTests
{
    private Codec8ExtendedParser uut;

    public Codec8ExtendedHandlerTests()
    {
        uut = new();
    }

    [Fact]
    public void ReadHeader()
    {
        var bytes = Messages.Example1Codec8Extended.ToByteArray();
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));

        var header = uut.ReadHeader(ref reader);
        var records = uut.ReadRecords(ref reader, header.NumberOfData);
    }
}