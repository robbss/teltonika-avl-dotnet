using Teltonika.Avl.Elements.Data;
using Teltonika.Avl.Elements.Decoding;
using Teltonika.Avl.Elements.Models;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Elements;

public static class IoElementResolver
{
    public static ResolvedProperty? Resolve(IoProperty property)
    {
        var def = IoElementCatalog.Get(property.Id);
        if (def is null)
            return null;

        var value = ValueDecoder.DecodeValue(
            property.Value, def.DataType, def.Multiplier, def.EnumValues);

        return new ResolvedProperty(
            def.Id, def.Name, def.Group, value, def.Units, def.Description);
    }

    public static ResolvedProperty? Resolve(IoProperty property, TrackerModel model)
    {
        var def = IoElementCatalog.Get(property.Id, model);
        if (def is null)
            return null;

        var value = ValueDecoder.DecodeValue(
            property.Value, def.DataType, def.Multiplier, def.EnumValues);

        return new ResolvedProperty(
            def.Id, def.Name, def.Group, value, def.Units, def.Description);
    }

    public static bool TryResolve(IoProperty property, out ResolvedProperty resolved)
    {
        var result = Resolve(property);
        if (result.HasValue)
        {
            resolved = result.Value;
            return true;
        }

        resolved = default;
        return false;
    }

    public static bool TryResolve(IoProperty property, TrackerModel model, out ResolvedProperty resolved)
    {
        var result = Resolve(property, model);
        if (result.HasValue)
        {
            resolved = result.Value;
            return true;
        }

        resolved = default;
        return false;
    }

    public static IReadOnlyList<ResolvedProperty> ResolveAll(IoElement element)
    {
        var results = new List<ResolvedProperty>(element.Properties.Count);
        foreach (var prop in element.Properties)
        {
            if (Resolve(prop) is { } resolved)
                results.Add(resolved);
        }
        return results;
    }

    public static IReadOnlyList<ResolvedProperty> ResolveAll(IoElement element, TrackerModel model)
    {
        var results = new List<ResolvedProperty>(element.Properties.Count);
        foreach (var prop in element.Properties)
        {
            if (Resolve(prop, model) is { } resolved)
                results.Add(resolved);
        }
        return results;
    }

    public static IoElementDefinition? GetDefinition(ushort id) =>
        IoElementCatalog.Get(id);

    public static IoElementDefinition? GetDefinition(ushort id, TrackerModel model) =>
        IoElementCatalog.Get(id, model);

    public static bool IsSupported(ushort id, TrackerModel model) =>
        IoElementCatalog.Get(id, model)?.SupportedModels.Contains(model) ?? false;
}
