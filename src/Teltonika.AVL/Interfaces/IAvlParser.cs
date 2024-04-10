using System.Buffers;

namespace Teltonika.AVL;

public interface IAvlParser
{
    AvlRecord[] ReadRecords(ref SequenceReader<byte> reader, int count);
}