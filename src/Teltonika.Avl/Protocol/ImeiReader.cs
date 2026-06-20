using System.Buffers;
using System.Text;

namespace Teltonika.Avl.Protocol;

public static class ImeiReader
{
    public static bool TryRead(
        in ReadOnlySequence<byte> buffer,
        out string? imei,
        out SequencePosition consumed)
    {
        imei = null;
        consumed = buffer.Start;

        if (buffer.Length < 2)
            return false;

        var reader = new SequenceReader<byte>(buffer);
        reader.TryReadBigEndian(out short lengthRaw);
        ushort length = (ushort)lengthRaw;

        if (buffer.Length < 2 + length)
            return false;

        var imeiBytes = new byte[length];
        reader.TryCopyTo(imeiBytes);
        reader.Advance(length);

        imei = Encoding.ASCII.GetString(imeiBytes);
        consumed = reader.Position;
        return true;
    }
}
