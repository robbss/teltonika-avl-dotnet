using System.Collections.Frozen;
using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Elements.Data;

internal static partial class IoElementCatalog
{
    private static FrozenSet<TrackerModel> AllModels = null!;
    private static FrozenSet<TrackerModel> FmModels = null!;
    private static FrozenSet<TrackerModel> FmbModels = null!;
    private static FrozenSet<TrackerModel> FmcModels = null!;
    private static FrozenSet<TrackerModel> FmmModels = null!;

    private static void InitializeModelSets()
    {
        AllModels = Enum.GetValues<TrackerModel>().ToFrozenSet();

        FmModels = new[]
        {
            TrackerModel.FM1100, TrackerModel.FM1110, TrackerModel.FM1120, TrackerModel.FM1125,
            TrackerModel.FM1200, TrackerModel.FM1202, TrackerModel.FM1204,
            TrackerModel.FM2100, TrackerModel.FM2200,
            TrackerModel.FM3001, TrackerModel.FM3200, TrackerModel.FM3300, TrackerModel.FM3400,
            TrackerModel.FM3612, TrackerModel.FM3622, TrackerModel.FM3632,
            TrackerModel.FM4100, TrackerModel.FM4200,
            TrackerModel.FM5300, TrackerModel.FM5500,
            TrackerModel.FM6300, TrackerModel.FM6320
        }.ToFrozenSet();

        FmbModels = new[]
        {
            TrackerModel.FMB001, TrackerModel.FMB003, TrackerModel.FMB010, TrackerModel.FMB020,
            TrackerModel.FMB110, TrackerModel.FMB120, TrackerModel.FMB125, TrackerModel.FMB130,
            TrackerModel.FMB140, TrackerModel.FMB150, TrackerModel.FMB202, TrackerModel.FMB204,
            TrackerModel.FMB208, TrackerModel.FMB209, TrackerModel.FMB230, TrackerModel.FMB640,
            TrackerModel.FMB641, TrackerModel.FMB900, TrackerModel.FMB910, TrackerModel.FMB920,
            TrackerModel.FMB930, TrackerModel.FMB940, TrackerModel.FMB950, TrackerModel.FMB962,
            TrackerModel.FMB964, TrackerModel.FMB965
        }.ToFrozenSet();

        FmcModels = new[]
        {
            TrackerModel.FMC001, TrackerModel.FMC003, TrackerModel.FMC125, TrackerModel.FMC130,
            TrackerModel.FMC150, TrackerModel.FMC230, TrackerModel.FMC640, TrackerModel.FMC650,
            TrackerModel.FMC880
        }.ToFrozenSet();

        FmmModels = new[]
        {
            TrackerModel.FMM001, TrackerModel.FMM003, TrackerModel.FMM125, TrackerModel.FMM130,
            TrackerModel.FMM150, TrackerModel.FMM230, TrackerModel.FMM640, TrackerModel.FMM650,
            TrackerModel.FMM800, TrackerModel.FMM880
        }.ToFrozenSet();
    }
}
