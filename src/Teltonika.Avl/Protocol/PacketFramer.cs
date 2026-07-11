using System.Buffers.Binary;
using System.Text;

namespace Teltonika.Avl.Protocol;

public static class PacketFramer
{
    public static byte[] Frame(ReadOnlySpan<byte> dataField)
    {
        int dataLength = dataField.Length;
        var buffer = new byte[4 + 4 + dataLength + 4];
        var span = buffer.AsSpan();

        // Preamble (4 zero bytes) — already zeroed by allocation

        // Data length
        BinaryPrimitives.WriteInt32BigEndian(span[4..], dataLength);

        // Data field
        dataField.CopyTo(span[8..]);

        // CRC-16 over data field
        ushort crc = Crc16Ibm.Compute(dataField);
        BinaryPrimitives.WriteInt32BigEndian(span[(8 + dataLength)..], crc);

        return buffer;
    }

    /// <summary>
    /// Allocates a full packet buffer with the length header written; the caller fills
    /// the data field at offset 8 and then calls <see cref="SealPacket"/> to write the CRC.
    /// </summary>
    internal static byte[] AllocatePacket(int dataFieldLength)
    {
        var buffer = new byte[4 + 4 + dataFieldLength + 4];
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(4), dataFieldLength);
        return buffer;
    }

    internal static void SealPacket(byte[] packet)
    {
        int dataLength = packet.Length - 12;
        ushort crc = Crc16Ibm.Compute(packet.AsSpan(8, dataLength));
        BinaryPrimitives.WriteInt32BigEndian(packet.AsSpan(8 + dataLength), crc);
    }

    public static byte[] EncodeImeiFrame(string imei)
    {
        var imeiBytes = Encoding.ASCII.GetBytes(imei);
        var buffer = new byte[2 + imeiBytes.Length];
        BinaryPrimitives.WriteInt16BigEndian(buffer, (short)imeiBytes.Length);
        imeiBytes.CopyTo(buffer.AsSpan(2));
        return buffer;
    }
}
