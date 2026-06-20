using System.Buffers;

namespace Teltonika.Avl.Protocol;

public static class Crc16Ibm
{
    private static readonly ushort[] Table = GenerateTable();

    private static ushort[] GenerateTable()
    {
        const ushort polynomial = 0xA001;
        var table = new ushort[256];

        for (int i = 0; i < 256; i++)
        {
            ushort crc = (ushort)i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (ushort)((crc >> 1) ^ polynomial);
                else
                    crc >>= 1;
            }
            table[i] = crc;
        }

        return table;
    }

    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;
        foreach (byte b in data)
        {
            byte index = (byte)(crc ^ b);
            crc = (ushort)((crc >> 8) ^ Table[index]);
        }
        return crc;
    }

    public static ushort Compute(in ReadOnlySequence<byte> data)
    {
        ushort crc = 0;
        foreach (ReadOnlyMemory<byte> segment in data)
        {
            foreach (byte b in segment.Span)
            {
                byte index = (byte)(crc ^ b);
                crc = (ushort)((crc >> 8) ^ Table[index]);
            }
        }
        return crc;
    }
}
