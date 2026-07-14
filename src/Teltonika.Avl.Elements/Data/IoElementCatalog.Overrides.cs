using System.Collections.Frozen;
using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Elements.Data;

internal static partial class IoElementCatalog
{
    private static FrozenDictionary<(ushort Id, TrackerModel Model), IoElementDefinition> BuildOverrides()
    {
        var overrides = new Dictionary<(ushort, TrackerModel), IoElementDefinition>();

        var buttonClickDef = Def(389, "Button Click", IoDataType.Unsigned, 1, group: "Eventual",
            enumValues: new Dictionary<int, string>
            {
                [18] = "Alarm Button Single Click",
                [19] = "Alarm Button Double Click",
                [33] = "Power Button Single Click",
                [34] = "Power Button Double Click"
            });

        AddOverride(overrides, 389, TrackerModel.TMT250, buttonClickDef);
        AddOverride(overrides, 389, TrackerModel.GH5200, buttonClickDef);

        // IDs 12, 13: "Fuel Used GPS" / "Average Fuel Use" on FMB/FMC/FMM,
        // but "Battery Voltage" / "Battery Current" on FM6300/FM6320
        var fm63Models = new[] { TrackerModel.FM6300, TrackerModel.FM6320 }.ToFrozenSet();

        AddOverrides(overrides, 12, fm63Models,
            Def(12, "Battery Voltage", IoDataType.Unsigned, 2, units: "mV", group: "Permanent",
                supportedModels: fm63Models));

        AddOverrides(overrides, 13, fm63Models,
            Def(13, "Battery Current", IoDataType.Unsigned, 2, units: "mA", group: "Permanent",
                supportedModels: fm63Models));

        // ID 217: "OBD Protocol" on most models, but "RFID COM2" (card ID from an
        // RS-232 reader on COM2) on the FMx640 family
        var fm64Models = new[] { TrackerModel.FMB640, TrackerModel.FMC640, TrackerModel.FMM640 }.ToFrozenSet();

        AddOverrides(overrides, 217, fm64Models,
            Def(217, "RFID COM2", IoDataType.Unsigned, 8, group: "Eventual",
                supportedModels: fm64Models));

        // IDs 246, 252: 1-byte Boolean on most models, but 2-byte Unsigned on FMC003
        var fmc003 = new[] { TrackerModel.FMC003 }.ToFrozenSet();

        AddOverrides(overrides, 246, fmc003,
            Def(246, "Towing", IoDataType.Unsigned, 2, group: "Eventual",
                supportedModels: fmc003));

        AddOverrides(overrides, 252, fmc003,
            Def(252, "Unplug", IoDataType.Unsigned, 2, group: "Eventual",
                supportedModels: fmc003));

        return overrides.ToFrozenDictionary();
    }

    private static void AddOverride(
        Dictionary<(ushort, TrackerModel), IoElementDefinition> dict,
        ushort id,
        TrackerModel model,
        IoElementDefinition definition)
    {
        dict[(id, model)] = definition;
    }

    private static void AddOverrides(
        Dictionary<(ushort, TrackerModel), IoElementDefinition> dict,
        ushort id,
        FrozenSet<TrackerModel> models,
        IoElementDefinition definition)
    {
        foreach (var model in models)
            dict[(id, model)] = definition;
    }
}
