using System.Collections.Frozen;
using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Elements.Data;

internal static partial class IoElementCatalog
{
    private static FrozenSet<TrackerModel> AllModels = null!;

    private static void InitializeModelSets()
    {
        AllModels = Enum.GetValues<TrackerModel>().ToFrozenSet();
    }
}
