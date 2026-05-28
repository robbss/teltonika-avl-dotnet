using Teltonika.AVL.Models;

namespace Teltonika.AVL.Protocol;

public interface ICodecParser
{
    AvlCodec CodecId { get; }

    bool TryParse(ReadOnlySpan<byte> payload, out AvlDataPacket? packet);
}