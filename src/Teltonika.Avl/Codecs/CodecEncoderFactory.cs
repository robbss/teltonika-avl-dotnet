using Teltonika.Avl.Models;

namespace Teltonika.Avl.Codecs;

public static class CodecEncoderFactory
{
    public static IDataCodecEncoder GetDataEncoder(CodecId codecId) => codecId switch
    {
        CodecId.Codec8 => Codec8Encoder.Instance,
        CodecId.Codec8Extended => Codec8ExtendedEncoder.Instance,
        CodecId.Codec16 => Codec16Encoder.Instance,
        _ => throw new NotSupportedException($"Codec {codecId} is not a data codec")
    };

    public static ICommandCodecEncoder GetCommandEncoder(CodecId codecId) => codecId switch
    {
        CodecId.Codec12 => Codec12Encoder.Instance,
        CodecId.Codec13 => Codec13Encoder.Instance,
        CodecId.Codec14 => Codec14Encoder.Instance,
        _ => throw new NotSupportedException($"Codec {codecId} is not a command codec")
    };
}
