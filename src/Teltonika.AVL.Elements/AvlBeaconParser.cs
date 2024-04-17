using System.Buffers;
using Teltonika.AVL.Elements.Devices.Beacons;

namespace Teltonika.AVL.Elements;

public class AvlBeaconParser
{
    public static List<TeltonikaBeacon> ParseBeacons(AvlIOElement element)
    {
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(element.Value));

        while (!reader.End)

        throw new NotImplementedException();
    }
}