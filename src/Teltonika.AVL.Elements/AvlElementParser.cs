using System.Buffers.Binary;
using System.Reflection;
using Teltonika.AVL.Attributes;
using Teltonika.AVL.Elements.Devices.Beacons;
using Teltonika.AVL.Elements.Parsers;

namespace Teltonika.AVL.Elements;

public class AvlElementParser
{
    public static List<AvlElement> ParseElements(DeviceType deviceType, AvlIOElement[] elements)
    {
        var result = new List<AvlElement>();
        var device = GetDevice(deviceType);

        foreach (var element in elements)
        {
            if (device.Properties.TryGetValue(element.Id, out var propertyInfo))
            {
                result.Add(GetElement(element, propertyInfo));
            }
            else
            {
                throw new NotSupportedException($"Attempted to parse non-supported IO: {element.Id}");
            }
        }

        return result;
    }

    private static AvlElement GetElement(AvlIOElement element, TeltonikaDevice.DeviceProperty propertyInfo)
    {
        if (propertyInfo.Type == typeof(byte))
        {
            return new AvlElement<byte>(element.Id, element.Value.First()) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }
        if (propertyInfo.Type == typeof(sbyte))
        {
            return new AvlElement<sbyte>(element.Id, (sbyte)element.Value.First()) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }

        if (propertyInfo.Type == typeof(short))
        {
            return new AvlElement<short>(element.Id, BinaryPrimitives.ReadInt16BigEndian(element.Value)) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }
        if (propertyInfo.Type == typeof(ushort))
        {
            return new AvlElement<ushort>(element.Id, BinaryPrimitives.ReadUInt16BigEndian(element.Value)) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }

        if (propertyInfo.Type == typeof(int))
        {
            return new AvlElement<int>(element.Id, BinaryPrimitives.ReadInt32BigEndian(element.Value)) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }
        if (propertyInfo.Type == typeof(uint))
        {
            return new AvlElement<uint>(element.Id, BinaryPrimitives.ReadUInt32BigEndian(element.Value)) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }

        if (propertyInfo.Type == typeof(long))
        {
            return new AvlElement<long>(element.Id, BinaryPrimitives.ReadInt64BigEndian(element.Value)) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }
        if (propertyInfo.Type == typeof(ulong))
        {
            return new AvlElement<ulong>(element.Id, BinaryPrimitives.ReadUInt64BigEndian(element.Value)) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }

        if (propertyInfo.Type == typeof(TeltonikaBeacon))
        {
            return new AvlElement<List<TeltonikaBeacon>>(element.Id, AvlBeaconParser.ParseBeacons(element.Value)) { Name = propertyInfo.Name, Multiplier = propertyInfo.Multiplier, Unit = propertyInfo.Unit };
        }

        throw new NotSupportedException();
    }

    private static TeltonikaDevice GetDevice(DeviceType deviceType)
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetExportedTypes())
        {
            var attr = type.GetCustomAttribute<TeltonikaDeviceAttribute>();

            if (attr is not null && attr.DeviceType == deviceType)
            {
                return Activator.CreateInstance(type) as TeltonikaDevice ?? new TeltonikaDevice();
            }
        }

        return new TeltonikaDevice();
    }
}