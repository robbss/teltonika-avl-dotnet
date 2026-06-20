using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

public interface IDataCodecEncoder
{
    byte[] EncodeDataPacket(AvlPacket packet);
}

public interface ICommandCodecEncoder
{
    byte[] EncodeCommandPacket(GprsCommandPacket command);
}
