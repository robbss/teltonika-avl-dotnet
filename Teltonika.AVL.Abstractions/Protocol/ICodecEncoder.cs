using Teltonika.AVL.Models;

namespace Teltonika.AVL.Protocol;

public interface ICodecEncoder
{
    AvlCodec CodecId { get; }

    byte[] Encode(AvlDataPacket packet);
}