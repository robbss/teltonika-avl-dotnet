using System.Buffers.Binary;
using System.Text;

namespace Teltonika.Avl.Protocol;

/// <summary>Sequential big-endian writer over a pre-sized span.</summary>
internal ref struct SpanWriter(Span<byte> destination)
{
    private readonly Span<byte> _span = destination;
    private int _position;

    public readonly int Position => _position;

    public void WriteByte(byte value) => _span[_position++] = value;

    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(_span[_position..]);
        _position += bytes.Length;
    }

    public void WriteAscii(string text) =>
        _position += Encoding.ASCII.GetBytes(text, _span[_position..]);

    public void WriteUInt16(ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(_span[_position..], value);
        _position += 2;
    }

    public void WriteInt16(short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(_span[_position..], value);
        _position += 2;
    }

    public void WriteInt32(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(_span[_position..], value);
        _position += 4;
    }

    public void WriteInt64(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(_span[_position..], value);
        _position += 8;
    }
}
