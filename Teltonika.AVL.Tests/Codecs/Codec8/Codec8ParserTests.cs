using System.Buffers;
using Teltonika.AVL.Codecs;

namespace Teltonika.AVL.Tests.Codecs.Codec8;

public class Codec8ParserTests
{
    private Codec8Parser uut;

    public Codec8ParserTests()
    {
        uut = new();
    }

    [Fact]
    public void ReadHeader()
    {
        var bytes = Messages.Example3Codec8.ToByteArray();
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));

        //var header = uut.ReadHeader(ref reader);
        var records = uut.ReadRecords(ref reader, header.NumberOfData);
    }
}