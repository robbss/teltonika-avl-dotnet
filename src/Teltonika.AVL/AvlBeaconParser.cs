using System.Buffers;
using Teltonika.AVL.Beacons;
using Teltonika.AVL.Elements;
using Teltonika.AVL.Extensions;

namespace Teltonika.AVL;

public class AvlBeaconParser
{
    public static List<TeltonikaBeacon> Parse(byte[] buffer)
    {
        if (buffer.Length == 1)
        {
            return [];
        }

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));
        var beacons = new List<TeltonikaBeacon>();

        reader.Advance(1);

        while (reader.TryPeek(out _))
        {
            var flags = (BeaconFlags)reader.ReadByte();

            if (flags.HasFlag(BeaconFlags.iBeacon))
            {
                beacons.Add(ParseIBeacon(ref reader, flags));
            }
            else if (flags.HasFlag(BeaconFlags.Eddystone))
            {
                beacons.Add(ParseEddystone(ref reader, flags));
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        return beacons;
    }

    private static Eddystone ParseEddystone(ref SequenceReader<byte> reader, BeaconFlags flags)
    {
        return new Eddystone()
        {
            Namespace = reader.ReadByteArray(10),
            InstanceId = reader.ReadByteArray(6),
            TxPower = (sbyte)reader.ReadByte(),
            BatteryVoltage = flags.HasFlag(BeaconFlags.Battery) ? reader.ReadShortBigEndian() : null,
            Temperature = flags.HasFlag(BeaconFlags.Temperature) ? reader.ReadShortBigEndian() : null
        };
    }

    private static IBeacon ParseIBeacon(ref SequenceReader<byte> reader, BeaconFlags flags)
    {
        return new IBeacon()
        {
            Uuid = new Guid(reader.ReadIntBigEndian(), reader.ReadShortBigEndian(), reader.ReadShortBigEndian(), reader.ReadByteArray(8)),
            Major = (ushort)reader.ReadShortBigEndian(),
            Minor = (ushort)reader.ReadShortBigEndian(),
            TxPower = (sbyte)reader.ReadByte()
        };
    }
}