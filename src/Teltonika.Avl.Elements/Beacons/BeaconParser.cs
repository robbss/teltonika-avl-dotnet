using System.Buffers.Binary;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Elements.Beacons;

/// <summary>
/// Decodes the Teltonika beacon IO elements: AVL ID 385 ("Beacon", simple mode)
/// and AVL ID 548 ("Advanced BLE Beacon Data", advanced mode).
/// </summary>
public static class BeaconParser
{
    /// <summary>AVL ID of the simple-mode beacon list element.</summary>
    public const ushort BeaconListId = 385;

    /// <summary>AVL ID of the advanced-mode beacon data element.</summary>
    public const ushort AdvancedBeaconId = 548;

    /// <summary>
    /// Parses a simple-mode beacon list payload (the value of IO property 385).
    /// </summary>
    /// <exception cref="InvalidDataException">The payload is malformed or truncated.</exception>
    public static BeaconList ParseBeaconList(ReadOnlyMemory<byte> data) =>
        TryParseBeaconList(data, out var list, out var error) ? list! : throw new InvalidDataException(error);

    /// <summary>
    /// Parses a simple-mode beacon list payload, returning false instead of throwing on malformed input.
    /// </summary>
    public static bool TryParseBeaconList(ReadOnlyMemory<byte> data, out BeaconList? list) =>
        TryParseBeaconList(data, out list, out _);

    private static bool TryParseBeaconList(ReadOnlyMemory<byte> data, out BeaconList? list, out string? error)
    {
        error = null;

        if (data.IsEmpty)
        {
            list = new BeaconList(0, 0, []);
            return true;
        }

        list = null;
        var span = data.Span;
        byte dataPart = span[0];
        var beacons = new List<Beacon>();
        int offset = 1;

        while (offset < span.Length)
        {
            var flags = (BeaconFlags)span[offset++];
            bool isIBeacon = flags.HasFlag(BeaconFlags.IBeacon);

            int idLength = isIBeacon ? 20 : 16;
            int optionalLength =
                (flags.HasFlag(BeaconFlags.Rssi) ? 1 : 0) +
                (flags.HasFlag(BeaconFlags.BatteryVoltage) ? 2 : 0) +
                (flags.HasFlag(BeaconFlags.Temperature) ? 2 : 0);

            if (offset + idLength + optionalLength > span.Length)
            {
                error = $"Truncated beacon record at offset {offset - 1}";
                return false;
            }

            Beacon beacon = isIBeacon
                ? new IBeacon(
                    new Guid(span.Slice(offset, 16), bigEndian: true),
                    BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset + 16)),
                    BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset + 18)))
                : new Eddystone(data.Slice(offset, 10), data.Slice(offset + 10, 6));
            offset += idLength;

            if (flags.HasFlag(BeaconFlags.Rssi))
                beacon = beacon with { Rssi = (sbyte)span[offset++] };

            if (flags.HasFlag(BeaconFlags.BatteryVoltage))
            {
                beacon = beacon with { BatteryVoltage = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset)) };
                offset += 2;
            }

            if (flags.HasFlag(BeaconFlags.Temperature))
            {
                beacon = beacon with { Temperature = BinaryPrimitives.ReadInt16BigEndian(span.Slice(offset)) };
                offset += 2;
            }

            beacons.Add(beacon);
        }

        list = new BeaconList((byte)(dataPart >> 4), (byte)(dataPart & 0x0F), beacons);
        return true;
    }

    /// <summary>
    /// Parses an advanced-mode beacon payload (the value of IO property 548).
    /// Unknown TLV parameter ids are skipped.
    /// </summary>
    /// <exception cref="InvalidDataException">The payload is malformed or truncated.</exception>
    public static IReadOnlyList<AdvancedBeacon> ParseAdvancedBeacons(ReadOnlyMemory<byte> data) =>
        TryParseAdvancedBeacons(data, out var beacons, out var error) ? beacons! : throw new InvalidDataException(error);

    /// <summary>
    /// Parses an advanced-mode beacon payload, returning false instead of throwing on malformed input.
    /// </summary>
    public static bool TryParseAdvancedBeacons(ReadOnlyMemory<byte> data, out IReadOnlyList<AdvancedBeacon>? beacons) =>
        TryParseAdvancedBeacons(data, out beacons, out _);

    private static bool TryParseAdvancedBeacons(ReadOnlyMemory<byte> data, out IReadOnlyList<AdvancedBeacon>? beacons, out string? error)
    {
        error = null;

        if (data.IsEmpty)
        {
            beacons = [];
            return true;
        }

        beacons = null;
        var span = data.Span;
        var results = new List<AdvancedBeacon>();
        int offset = 1; // constant 0x01 header byte

        while (offset < span.Length)
        {
            int recordEnd = offset + 1 + span[offset];
            offset++;
            if (recordEnd > span.Length)
            {
                error = $"Truncated beacon record at offset {offset - 1}";
                return false;
            }

            sbyte? rssi = null;
            ReadOnlyMemory<byte> beaconId = default;
            ReadOnlyMemory<byte> additionalData = default;

            while (offset < recordEnd)
            {
                if (recordEnd - offset < 2)
                {
                    error = $"Truncated TLV header at offset {offset}";
                    return false;
                }

                byte parameter = span[offset++];
                byte length = span[offset++];
                if (offset + length > recordEnd)
                {
                    error = $"TLV value overruns beacon record at offset {offset}";
                    return false;
                }

                switch (parameter)
                {
                    case 0x00 when length >= 1:
                        rssi = (sbyte)span[offset];
                        break;
                    case 0x01:
                        beaconId = data.Slice(offset, length);
                        break;
                    case 0x02:
                        additionalData = data.Slice(offset, length);
                        break;
                }

                offset += length;
            }

            results.Add(new AdvancedBeacon(rssi, beaconId, additionalData));
        }

        beacons = results;
        return true;
    }

    /// <summary>
    /// Finds and parses the simple-mode beacon property (AVL ID 385) on an IO element,
    /// or returns null when the element carries no beacon list.
    /// </summary>
    public static BeaconList? GetBeacons(IoElement element)
    {
        foreach (var property in element.Properties)
        {
            if (property.Id == BeaconListId)
                return ParseBeaconList(property.Value);
        }

        return null;
    }

    /// <summary>
    /// Finds and parses the advanced-mode beacon property (AVL ID 548) on an IO element,
    /// or returns null when the element carries no advanced beacon data.
    /// </summary>
    public static IReadOnlyList<AdvancedBeacon>? GetAdvancedBeacons(IoElement element)
    {
        foreach (var property in element.Properties)
        {
            if (property.Id == AdvancedBeaconId)
                return ParseAdvancedBeacons(property.Value);
        }

        return null;
    }
}
