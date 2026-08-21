using Teltonika.Avl.Elements.Data;
using Teltonika.Avl.Elements.Decoding;
using Teltonika.Avl.Elements.Encoding;
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

    /// <summary>Every known IO element definition, keyed by AVL ID.</summary>
    public static IReadOnlyDictionary<ushort, IoElementDefinition> Definitions =>
        IoElementCatalog.All;

    /// <summary>Encodes a logical value as the raw IO property a device would send.</summary>
    public static IoProperty Encode(ushort id, object value) =>
        Encode(IoElementCatalog.Get(id), id, value);

    /// <inheritdoc cref="Encode(ushort, object)"/>
    public static IoProperty Encode(ushort id, object value, TrackerModel model) =>
        Encode(IoElementCatalog.Get(id, model), id, value);

    public static bool TryEncode(ushort id, object value, out IoProperty property) =>
        TryEncode(IoElementCatalog.Get(id), id, value, out property);

    public static bool TryEncode(ushort id, object value, TrackerModel model, out IoProperty property) =>
        TryEncode(IoElementCatalog.Get(id, model), id, value, out property);

    private static IoProperty Encode(IoElementDefinition? definition, ushort id, object value)
    {
        if (definition is null)
            throw new ArgumentException($"Unknown IO element ID {id}.", nameof(id));

        return new IoProperty(id, ValueEncoder.EncodeValue(
            value, definition.DataType, definition.ValueSize, definition.Multiplier, definition.EnumValues));
    }

    private static bool TryEncode(IoElementDefinition? definition, ushort id, object value, out IoProperty property)
    {
        if (definition is null)
        {
            property = default;
            return false;
        }

        property = Encode(definition, id, value);
        return true;
    }
}
