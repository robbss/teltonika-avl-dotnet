using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

internal static class CodecDecoderFactory
{
    public static ICodecDecoder GetDataDecoder(CodecId codecId) => codecId switch
    {
        CodecId.Codec8 => Codec8Decoder.Instance,
        CodecId.Codec8Extended => Codec8ExtendedDecoder.Instance,
        CodecId.Codec16 => Codec16Decoder.Instance,
        _ => throw new NotSupportedException($"Codec {codecId} is not a data codec")
    };

    public static ICommandCodecDecoder GetCommandDecoder(CodecId codecId) => codecId switch
    {
        CodecId.Codec12 => Codec12Decoder.Instance,
        CodecId.Codec13 => Codec13Decoder.Instance,
        CodecId.Codec14 => Codec14Decoder.Instance,
        _ => throw new NotSupportedException($"Codec {codecId} is not a command codec")
    };

    public static bool IsDataCodec(byte codecByte) =>
        codecByte is 0x08 or 0x8E or 0x10;

    public static bool IsCommandCodec(byte codecByte) =>
        codecByte is 0x0C or 0x0D or 0x0E;
}
