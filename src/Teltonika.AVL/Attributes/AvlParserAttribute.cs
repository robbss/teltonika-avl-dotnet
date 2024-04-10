namespace Teltonika.AVL.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AvlParserAttribute : Attribute
{
    public AvlParserAttribute(AvlCodec codec)
    {
        CodecId = (byte)codec;
    }

    public AvlParserAttribute(byte codecId)
    {
        CodecId = codecId;
    }

    public byte CodecId { get; }
}