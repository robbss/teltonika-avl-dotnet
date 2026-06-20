using System.Buffers;
using System.Text;
using Teltonika.Avl.Codecs;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl;

public static class AvlParser
{
    public static AvlPacket Parse(ReadOnlySpan<byte> rawPacket)
    {
        var sequence = new ReadOnlySequence<byte>(rawPacket.ToArray());

        if (!AvlPacketReader.TryReadPacket(in sequence, out var dataField, out _, out _))
            throw new InvalidDataException("Incomplete packet");

        var reader = new SequenceReader<byte>(dataField);

        if (!reader.TryPeek(out byte codecByte))
            throw new InvalidDataException("Empty data field");

        if (CodecDecoderFactory.IsDataCodec(codecByte))
        {
            var decoder = CodecDecoderFactory.GetDataDecoder((CodecId)codecByte);
            return decoder.DecodeDataPacket(ref reader);
        }

        throw new NotSupportedException(
            $"Codec 0x{codecByte:X2} is not a data codec. Use ParseCommand for command codecs.");
    }

    public static bool TryParse(ReadOnlySpan<byte> rawPacket, out AvlPacket? packet)
    {
        packet = null;
        try
        {
            packet = Parse(rawPacket);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static GprsCommandPacket ParseCommand(ReadOnlySpan<byte> rawPacket)
    {
        var sequence = new ReadOnlySequence<byte>(rawPacket.ToArray());

        if (!AvlPacketReader.TryReadPacket(in sequence, out var dataField, out _, out _))
            throw new InvalidDataException("Incomplete packet");

        var reader = new SequenceReader<byte>(dataField);

        if (!reader.TryPeek(out byte codecByte))
            throw new InvalidDataException("Empty data field");

        if (CodecDecoderFactory.IsCommandCodec(codecByte))
        {
            var decoder = CodecDecoderFactory.GetCommandDecoder((CodecId)codecByte);
            return decoder.DecodeCommandPacket(ref reader);
        }

        throw new NotSupportedException(
            $"Codec 0x{codecByte:X2} is not a command codec. Use Parse for data codecs.");
    }

    public static string ParseImei(ReadOnlySpan<byte> imeiFrame)
    {
        var sequence = new ReadOnlySequence<byte>(imeiFrame.ToArray());
        if (!ImeiReader.TryRead(in sequence, out string? imei, out _))
            throw new InvalidDataException("Incomplete IMEI frame");
        return imei!;
    }
}
