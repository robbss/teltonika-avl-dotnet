using Teltonika.AVL.Models;
using Teltonika.AVL.Profiles;

namespace Teltonika.AVL.Protocol;

public interface IIoElementsParser
{
    IReadOnlyList<IoElement> Parse(RawIoElement rawIo, IIoProfile profile);
}