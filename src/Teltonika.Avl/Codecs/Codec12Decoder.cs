using System.Buffers;
using System.Text;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal sealed class Codec12Decoder : ICommandCodecDecoder
{
    public static readonly Codec12Decoder Instance = new();

    public bool TryDecodeCommandPacket(ref SequenceReader<byte> reader, out GprsCommandPacket? packet, out string? error)
    {
        packet = null;

        if (!reader.TryRead(out _) ||
            !reader.TryRead(out byte commandCount) ||
            !reader.TryRead(out byte commandType) ||
            !reader.TryReadBigEndian(out int commandSize))
        {
            error = Codec8Decoder.TruncatedError;
            return false;
        }

        if (commandSize < 0 || commandSize > reader.Remaining)
        {
            error = $"Invalid command size: {commandSize}";
            return false;
        }

        var commandBytes = new byte[commandSize];
        reader.TryCopyTo(commandBytes);
        reader.Advance(commandSize);

        if (!reader.TryRead(out byte commandCount2))
        {
            error = Codec8Decoder.TruncatedError;
            return false;
        }

        if (commandCount != commandCount2)
        {
            error = $"Command count mismatch: {commandCount} != {commandCount2}";
            return false;
        }

        packet = new GprsCommandPacket(CodecId.Codec12, commandType, Encoding.ASCII.GetString(commandBytes));
        error = null;
        return true;
    }
}
