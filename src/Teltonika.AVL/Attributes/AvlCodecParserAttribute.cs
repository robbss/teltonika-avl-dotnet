namespace Teltonika.AVL.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AvlCodecParserAttribute : Attribute
{
    public AvlCodecParserAttribute(AvlCodec codec)
    {
        CodecId = (byte)codec;
    }

    public AvlCodecParserAttribute(byte codecId)
    {
        CodecId = codecId;
    }

    public byte CodecId { get; }
}