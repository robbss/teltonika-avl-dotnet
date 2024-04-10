using System.Buffers;
using System.Runtime.CompilerServices;

namespace Teltonika.AVL.Extensions;

public static class SequenceReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadByteArray(ref this SequenceReader<byte> reader, int count)
    {
        if (!reader.TryReadExact(count, out var sequence))
        {
            throw new Exception();
        }

        return sequence.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadLongBigEndian(ref this SequenceReader<byte> reader)
    {
        if (!reader.TryReadBigEndian(out long value))
        {
            throw new Exception();
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ReadShortBigEndian(ref this SequenceReader<byte> reader)
    {
        if (!reader.TryReadBigEndian(out short value))
        {
            throw new Exception();
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadIntBigEndian(ref this SequenceReader<byte> reader)
    {
        if (!reader.TryReadBigEndian(out int value))
        {
            throw new Exception();
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(ref this SequenceReader<byte> reader)
    {
        if (!reader.TryRead(out byte value))
        {
            throw new Exception();
        }

        return value;
    }
}