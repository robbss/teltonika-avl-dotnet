using System.Buffers;

namespace Teltonika.AVL;

public interface IAvlCodec
{
    AvlRecord[] Parse(ref SequenceReader<byte> reader, int count);
}