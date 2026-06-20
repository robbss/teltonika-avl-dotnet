using System.Collections.Frozen;

namespace Teltonika.Avl.Elements.Models;

public sealed record IoElementDefinition(
    ushort Id,
    string Name,
    IoDataType DataType,
    byte ValueSize,
    double Multiplier,
    string? Units,
    double? Min,
    double? Max,
    string? Group,
    string? Description,
    FrozenDictionary<int, string>? EnumValues,
    FrozenSet<TrackerModel> SupportedModels);
