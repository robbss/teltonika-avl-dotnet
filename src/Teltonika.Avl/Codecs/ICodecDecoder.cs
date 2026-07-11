using System.Buffers;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal interface ICodecDecoder
{
    /// <summary>
    /// Decodes a data packet without throwing. Returns false with a diagnostic
    /// <paramref name="error"/> when the data field is malformed.
    /// </summary>
    bool TryDecodeDataPacket(ref SequenceReader<byte> reader, out AvlPacket? packet, out string? error);
}

internal interface ICommandCodecDecoder
{
    /// <summary>
    /// Decodes a GPRS command packet without throwing. Returns false with a diagnostic
    /// <paramref name="error"/> when the data field is malformed.
    /// </summary>
    bool TryDecodeCommandPacket(ref SequenceReader<byte> reader, out GprsCommandPacket? packet, out string? error);
}
