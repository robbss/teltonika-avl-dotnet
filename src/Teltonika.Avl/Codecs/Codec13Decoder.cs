using System.Buffers;
using System.Text;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal sealed class Codec13Decoder : ICommandCodecDecoder
{
    public static readonly Codec13Decoder Instance = new();

    public GprsCommandPacket DecodeCommandPacket(ref SequenceReader<byte> reader)
    {
        reader.TryRead(out byte codecByte);
        reader.TryRead(out byte commandCount);

        reader.TryRead(out byte commandType);
        reader.TryReadBigEndian(out int timestampSeconds);
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);

        reader.TryReadBigEndian(out int commandSize);

        var commandBytes = new byte[commandSize];
        reader.TryCopyTo(commandBytes);
        reader.Advance(commandSize);
        string commandText = Encoding.ASCII.GetString(commandBytes);

        reader.TryRead(out byte commandCount2);
        if (commandCount != commandCount2)
            throw new InvalidDataException($"Command count mismatch: {commandCount} != {commandCount2}");

        return new GprsCommandPacket(CodecId.Codec13, commandType, commandText, Timestamp: timestamp);
    }
}
