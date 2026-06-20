using System.Buffers;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal interface ICodecDecoder
{
    AvlPacket DecodeDataPacket(ref SequenceReader<byte> reader);
}

internal interface ICommandCodecDecoder
{
    GprsCommandPacket DecodeCommandPacket(ref SequenceReader<byte> reader);
}
