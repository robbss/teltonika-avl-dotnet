using System.Buffers;
using System.Text;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal sealed class Codec14Decoder : ICommandCodecDecoder
{
    public static readonly Codec14Decoder Instance = new();

    public bool TryDecodeCommandPacket(ref SequenceReader<byte> reader, out GprsCommandPacket? packet, out string? error)
    {
        packet = null;

        // Command size includes the 8-byte packed IMEI that precedes the command text
        if (!reader.TryRead(out _) ||
            !reader.TryRead(out byte commandCount) ||
            !reader.TryRead(out byte commandType) ||
            !reader.TryReadBigEndian(out int commandSize))
        {
            error = Codec8Decoder.TruncatedError;
            return false;
        }

        if (commandSize < 8 || commandSize > reader.Remaining)
        {
            error = $"Invalid Codec 14 command size: {commandSize} (must be at least 8 for the IMEI)";
            return false;
        }

        Span<byte> imeiBytes = stackalloc byte[8];
        reader.TryCopyTo(imeiBytes);
        reader.Advance(8);

        // IMEI is packed as 16 hex digits: a 15-digit IMEI padded with one leading zero
        string imeiHex = Convert.ToHexString(imeiBytes);
        string imei = imeiHex.StartsWith('0') ? imeiHex[1..] : imeiHex;

        var commandBytes = new byte[commandSize - 8];
        reader.TryCopyTo(commandBytes);
        reader.Advance(commandBytes.Length);

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

        packet = new GprsCommandPacket(
            CodecId.Codec14,
            commandType,
            Encoding.ASCII.GetString(commandBytes),
            Imei: imei);
        error = null;
        return true;
    }
}
