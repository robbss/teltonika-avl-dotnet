using System.Buffers.Binary;

namespace Teltonika.AVL;

public class AvlElementParser
{
    public static AvlElement Parse(short id, byte[] buffer, AvlElementInfo elementInfo)
    {
        if (elementInfo.Type == typeof(byte))
        {
            return new AvlElement<byte>(id, buffer[0]) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }
        if (elementInfo.Type == typeof(sbyte))
        {
            return new AvlElement<sbyte>(id, (sbyte)buffer[0]) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }

        if (elementInfo.Type == typeof(short))
        {
            return new AvlElement<short>(id, BinaryPrimitives.ReadInt16BigEndian(buffer)) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }
        if (elementInfo.Type == typeof(ushort))
        {
            return new AvlElement<ushort>(id, BinaryPrimitives.ReadUInt16BigEndian(buffer)) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }

        if (elementInfo.Type == typeof(int))
        {
            return new AvlElement<int>(id, BinaryPrimitives.ReadInt32BigEndian(buffer)) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }
        if (elementInfo.Type == typeof(uint))
        {
            return new AvlElement<uint>(id, BinaryPrimitives.ReadUInt32BigEndian(buffer)) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }

        if (elementInfo.Type == typeof(long))
        {
            return new AvlElement<long>(id, BinaryPrimitives.ReadInt64BigEndian(buffer)) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }
        if (elementInfo.Type == typeof(ulong))
        {
            return new AvlElement<ulong>(id, BinaryPrimitives.ReadUInt64BigEndian(buffer)) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }

        if (elementInfo.Type == typeof(TeltonikaBeacon))
        {
            return new AvlElement<List<TeltonikaBeacon>>(id, AvlBeaconParser.Parse(buffer)) { Name = elementInfo.Name, Multiplier = elementInfo.Multiplier, Unit = elementInfo.Unit };
        }

        throw new NotSupportedException();
    }
}