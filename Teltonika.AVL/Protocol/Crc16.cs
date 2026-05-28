using System.Buffers;

namespace Teltonika.AVL.Protocol;

public static class Crc16
{
    private const ushort Polynomial = 0xA001;

    public static ushort Compute(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (ushort)((crc >> 1) ^ Polynomial);
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }

    public static ushort Compute(in ReadOnlySequence<byte> sequence)
    {
        ushort crc = 0;
        foreach (var memory in sequence)
        {
            foreach (byte b in memory.Span)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 1) != 0)
                    {
                        crc = (ushort)((crc >> 1) ^ Polynomial);
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
        }
        return crc;
    }
}