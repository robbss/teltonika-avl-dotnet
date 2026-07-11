using System.Buffers;
using Teltonika.Avl.Codecs;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl;

public static class AvlParser
{
    /// <summary>Parses a framed AVL data packet. Copies the span into a new buffer first.</summary>
    public static AvlPacket Parse(ReadOnlySpan<byte> rawPacket) =>
        Parse(new ReadOnlySequence<byte>(rawPacket.ToArray()));

    /// <summary>Parses a framed AVL data packet without copying the input.</summary>
    public static AvlPacket Parse(byte[] rawPacket) =>
        Parse(new ReadOnlySequence<byte>(rawPacket));

    /// <summary>Parses a framed AVL data packet without copying the input.</summary>
    public static AvlPacket Parse(ReadOnlyMemory<byte> rawPacket) =>
        Parse(new ReadOnlySequence<byte>(rawPacket));

    /// <summary>Parses a framed AVL data packet without copying the input.</summary>
    public static AvlPacket Parse(in ReadOnlySequence<byte> rawPacket)
    {
        if (!AvlPacketReader.TryReadPacket(in rawPacket, out var dataField, out _, out _, out var frameError))
            throw new InvalidDataException(frameError ?? "Incomplete packet");

        var reader = new SequenceReader<byte>(dataField);

        if (!reader.TryPeek(out byte codecByte))
            throw new InvalidDataException("Empty data field");

        if (!CodecDecoderFactory.IsDataCodec(codecByte))
        {
            throw new NotSupportedException(
                $"Codec 0x{codecByte:X2} is not a data codec. Use ParseCommand for command codecs.");
        }

        var decoder = CodecDecoderFactory.GetDataDecoder((CodecId)codecByte);
        if (!decoder.TryDecodeDataPacket(ref reader, out var packet, out var error))
            throw new InvalidDataException(error);

        return packet!;
    }

    public static bool TryParse(ReadOnlySpan<byte> rawPacket, out AvlPacket? packet) =>
        TryParse(new ReadOnlySequence<byte>(rawPacket.ToArray()), out packet);

    public static bool TryParse(byte[] rawPacket, out AvlPacket? packet) =>
        TryParse(new ReadOnlySequence<byte>(rawPacket), out packet);

    public static bool TryParse(ReadOnlyMemory<byte> rawPacket, out AvlPacket? packet) =>
        TryParse(new ReadOnlySequence<byte>(rawPacket), out packet);

    public static bool TryParse(in ReadOnlySequence<byte> rawPacket, out AvlPacket? packet)
    {
        packet = null;

        if (!AvlPacketReader.TryReadPacket(in rawPacket, out var dataField, out _, out _, out _))
            return false;

        var reader = new SequenceReader<byte>(dataField);

        if (!reader.TryPeek(out byte codecByte) || !CodecDecoderFactory.IsDataCodec(codecByte))
            return false;

        var decoder = CodecDecoderFactory.GetDataDecoder((CodecId)codecByte);
        return decoder.TryDecodeDataPacket(ref reader, out packet, out _);
    }

    /// <summary>Parses a framed GPRS command packet. Copies the span into a new buffer first.</summary>
    public static GprsCommandPacket ParseCommand(ReadOnlySpan<byte> rawPacket) =>
        ParseCommand(new ReadOnlySequence<byte>(rawPacket.ToArray()));

    /// <summary>Parses a framed GPRS command packet without copying the input.</summary>
    public static GprsCommandPacket ParseCommand(byte[] rawPacket) =>
        ParseCommand(new ReadOnlySequence<byte>(rawPacket));

    /// <summary>Parses a framed GPRS command packet without copying the input.</summary>
    public static GprsCommandPacket ParseCommand(ReadOnlyMemory<byte> rawPacket) =>
        ParseCommand(new ReadOnlySequence<byte>(rawPacket));

    /// <summary>Parses a framed GPRS command packet without copying the input.</summary>
    public static GprsCommandPacket ParseCommand(in ReadOnlySequence<byte> rawPacket)
    {
        if (!AvlPacketReader.TryReadPacket(in rawPacket, out var dataField, out _, out _, out var frameError))
            throw new InvalidDataException(frameError ?? "Incomplete packet");

        var reader = new SequenceReader<byte>(dataField);

        if (!reader.TryPeek(out byte codecByte))
            throw new InvalidDataException("Empty data field");

        if (!CodecDecoderFactory.IsCommandCodec(codecByte))
        {
            throw new NotSupportedException(
                $"Codec 0x{codecByte:X2} is not a command codec. Use Parse for data codecs.");
        }

        var decoder = CodecDecoderFactory.GetCommandDecoder((CodecId)codecByte);
        if (!decoder.TryDecodeCommandPacket(ref reader, out var packet, out var error))
            throw new InvalidDataException(error);

        return packet!;
    }

    public static bool TryParseCommand(ReadOnlySpan<byte> rawPacket, out GprsCommandPacket? packet) =>
        TryParseCommand(new ReadOnlySequence<byte>(rawPacket.ToArray()), out packet);

    public static bool TryParseCommand(byte[] rawPacket, out GprsCommandPacket? packet) =>
        TryParseCommand(new ReadOnlySequence<byte>(rawPacket), out packet);

    public static bool TryParseCommand(ReadOnlyMemory<byte> rawPacket, out GprsCommandPacket? packet) =>
        TryParseCommand(new ReadOnlySequence<byte>(rawPacket), out packet);

    public static bool TryParseCommand(in ReadOnlySequence<byte> rawPacket, out GprsCommandPacket? packet)
    {
        packet = null;

        if (!AvlPacketReader.TryReadPacket(in rawPacket, out var dataField, out _, out _, out _))
            return false;

        var reader = new SequenceReader<byte>(dataField);

        if (!reader.TryPeek(out byte codecByte) || !CodecDecoderFactory.IsCommandCodec(codecByte))
            return false;

        var decoder = CodecDecoderFactory.GetCommandDecoder((CodecId)codecByte);
        return decoder.TryDecodeCommandPacket(ref reader, out packet, out _);
    }

    /// <summary>Parses an IMEI handshake frame. Copies the span into a new buffer first.</summary>
    public static string ParseImei(ReadOnlySpan<byte> imeiFrame) =>
        ParseImei(new ReadOnlySequence<byte>(imeiFrame.ToArray()));

    /// <summary>Parses an IMEI handshake frame without copying the input.</summary>
    public static string ParseImei(byte[] imeiFrame) =>
        ParseImei(new ReadOnlySequence<byte>(imeiFrame));

    /// <summary>Parses an IMEI handshake frame without copying the input.</summary>
    public static string ParseImei(ReadOnlyMemory<byte> imeiFrame) =>
        ParseImei(new ReadOnlySequence<byte>(imeiFrame));

    /// <summary>Parses an IMEI handshake frame without copying the input.</summary>
    public static string ParseImei(in ReadOnlySequence<byte> imeiFrame)
    {
        if (!ImeiReader.TryRead(in imeiFrame, out string? imei, out _))
            throw new InvalidDataException("Incomplete IMEI frame");
        return imei!;
    }

    public static bool TryParseImei(ReadOnlySpan<byte> imeiFrame, out string? imei) =>
        ImeiReader.TryRead(new ReadOnlySequence<byte>(imeiFrame.ToArray()), out imei, out _);

    public static bool TryParseImei(byte[] imeiFrame, out string? imei) =>
        ImeiReader.TryRead(new ReadOnlySequence<byte>(imeiFrame), out imei, out _);

    public static bool TryParseImei(ReadOnlyMemory<byte> imeiFrame, out string? imei) =>
        ImeiReader.TryRead(new ReadOnlySequence<byte>(imeiFrame), out imei, out _);

    public static bool TryParseImei(in ReadOnlySequence<byte> imeiFrame, out string? imei) =>
        ImeiReader.TryRead(in imeiFrame, out imei, out _);
}
