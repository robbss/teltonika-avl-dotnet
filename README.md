# Teltonika AVL

A .NET 10 library for parsing, encoding, and serving [Teltonika](https://teltonika-gps.com/) GPS tracker data using the AVL protocol. Zero external dependencies for the core library.

## Features

- **Full codec support** &mdash; Codec 8, Codec 8 Extended, Codec 16 (data), Codec 12/13/14 (commands)
- **TCP and UDP channels** &mdash; Both envelopes, including the UDP acknowledgement
- **Parse & encode** &mdash; Symmetric API for reading and writing AVL packets
- **TCP server** &mdash; Production-ready async server with IMEI validation, idle timeouts, and bidirectional GPRS commands
- **IO element resolution** &mdash; Translate raw IO property IDs into named, typed values with units, using a built-in catalog of 150+ definitions across 78 tracker models
- **Per-model overrides** &mdash; Handles cases where the same AVL ID has different meanings on different hardware (e.g. ID 389 is "OBD Fuel Type" on FMB devices but "Button Click" on TMT250)
- **Beacon parsing** &mdash; Decode iBeacon/Eddystone beacon lists (AVL ID 385) and advanced beacon data (AVL ID 548)
- **Low allocation** &mdash; Copy-free `byte[]`/`ReadOnlyMemory<byte>`/`ReadOnlySequence<byte>` parse overloads, single-allocation encoders, `FrozenDictionary` lookups

## Projects

| Project | Description |
|---|---|
| `Teltonika.Avl` | Core parser, encoder, and TCP server |
| `Teltonika.Avl.Elements` | IO element catalog, property resolver, and beacon parser |

## Installation

Clone and build from source:

```bash
git clone <repo-url>
cd teltonika-avl-dotnet
dotnet build
```

## Quick Start

### Parsing a packet

```csharp
using Teltonika.Avl;

byte[] raw = /* bytes received from a tracker */;

// Parse a data packet (Codec 8, 8E, or 16 — auto-detected)
AvlPacket packet = AvlParser.Parse(raw);

Console.WriteLine($"Codec: {packet.CodecId}");
Console.WriteLine($"Records: {packet.Records.Count}");

foreach (var record in packet.Records)
{
    Console.WriteLine($"  Time: {record.Timestamp}");
    Console.WriteLine($"  GPS:  {record.Gps.Latitude}, {record.Gps.Longitude}");
    Console.WriteLine($"  Speed: {record.Gps.Speed} km/h, Alt: {record.Gps.Altitude} m");
    Console.WriteLine($"  IO properties: {record.IoData.Properties.Count}");
}
```

Use `TryParse` for non-throwing validation:

```csharp
if (AvlParser.TryParse(raw, out var packet))
{
    // valid packet
}
```

`Parse`, `TryParse`, `ParseCommand`, and `ParseImei` accept `byte[]`, `ReadOnlyMemory<byte>`, and `ReadOnlySequence<byte>` without copying the input; the `ReadOnlySpan<byte>` overloads copy the span into a temporary buffer first.

### Parsing commands and IMEI frames

```csharp
// Parse a GPRS command packet (Codec 12/13/14)
GprsCommandPacket cmd = AvlParser.ParseCommand(raw);
Console.WriteLine($"Command: {cmd.CommandText}");

// Parse an IMEI handshake frame
string imei = AvlParser.ParseImei(imeiBytes);
Console.WriteLine($"IMEI: {imei}");
```

### Encoding a packet

```csharp
using Teltonika.Avl;
using Teltonika.Avl.Models;

var packet = new AvlPacket(CodecId.Codec8Extended, new[]
{
    new AvlRecord(
        Timestamp: DateTimeOffset.UtcNow,
        Priority: Priority.High,
        Gps: new GpsData(
            Longitude: 25.2616,
            Latitude: 54.6993,
            Altitude: 200,
            Angle: 90,
            Satellites: 12,
            Speed: 60),
        IoData: new IoElement(0, new IoProperty[]
        {
            new(66, new byte[] { 0x2E, 0xE0 }),  // External Voltage: 12000 mV
            new(239, new byte[] { 0x01 }),          // Ignition: On
        }))
});

byte[] encoded = AvlEncoder.Encode(packet);
```

### Encoding commands and IMEI frames

```csharp
// Encode a GPRS command
var command = new GprsCommandPacket(CodecId.Codec12, 0x05, "getinfo");
byte[] cmdBytes = AvlEncoder.EncodeCommand(command);

// Encode an IMEI handshake frame
byte[] imeiFrame = AvlEncoder.EncodeImei("356307042441013");
```

## UDP channel

The UDP form is not the TCP form in a datagram. TCP frames a packet with a four-zero preamble, a length
prefix and a trailing CRC, and identifies the device once per connection with a separate IMEI handshake.
UDP has no connection to hang that on, so every datagram carries its own header &mdash; length, packet
id, AVL packet id and the IMEI inline &mdash; and drops the preamble and the CRC entirely.

```csharp
// Read a datagram: the IMEI comes with it, so no handshake state is needed
if (AvlParser.TryParseUdp(datagram, out var received))
{
    Console.WriteLine($"{received!.Imei}: {received.Packet.Records.Count} records");

    // Acknowledge it: both identifiers are echoed back so the device can match them
    byte[] ack = AvlEncoder.EncodeUdpAcknowledgement(received);
    await socket.SendToAsync(ack, remoteEndPoint);
}
```

```csharp
// Write one, as a device does
byte[] datagram = AvlEncoder.EncodeUdp(packet, imei: "352093086403655", packetId: 0xCAFE, avlPacketId: 0x05);
```

`UdpFramer` is the underlying type if you want the envelope without the facade. There is **no CRC** on a
UDP datagram &mdash; the protocol relies on UDP's own checksum, and the length field is the only
integrity check the envelope carries, which is worth knowing before looking for one.

UDP is the sensible transport for a high-device-count fleet: no socket per device, so no ephemeral-port
ceiling. The bundled server is TCP-only for now; the UDP channel is parse/encode support that you can
drive from any socket.

## TCP Server

The built-in `TeltonikaServer` handles the full device communication lifecycle: IMEI handshake, data ingestion, acknowledgements, and bidirectional GPRS commands.

```csharp
using Teltonika.Avl.Server;

await using var server = new TeltonikaServer(new TeltonikaServerOptions
{
    Port = 5027,
    MaxConnections = 500,
    IdleTimeout = TimeSpan.FromMinutes(5),
    ImeiValidator = (imei, ct) =>
    {
        // Accept or reject devices
        return ValueTask.FromResult(imei.StartsWith("356307"));
    }
});

server.DeviceConnected += (sender, e) =>
{
    Console.WriteLine($"Device connected: {e.Imei} from {e.RemoteEndPoint}");
    return Task.CompletedTask;
};

server.AvlDataReceived += (sender, e) =>
{
    Console.WriteLine($"[{e.Imei}] {e.Packet.Records.Count} records received");
    foreach (var record in e.Packet.Records)
    {
        Console.WriteLine($"  {record.Timestamp}: ({record.Gps.Latitude}, {record.Gps.Longitude})");
    }
    return Task.CompletedTask;
};

server.DeviceDisconnected += (sender, e) =>
{
    Console.WriteLine($"Device disconnected: {e.Imei} ({e.Reason})");
    return Task.CompletedTask;
};

await server.StartAsync();
Console.WriteLine($"Listening on port 5027...");

// Send a command to a connected device
await server.SendCommandAsync("356307042441013", "getinfo");

await Task.Delay(Timeout.Infinite);
```

### Active connections

```csharp
foreach (var (imei, connection) in server.Connections)
{
    Console.WriteLine($"{imei}: connected at {connection.ConnectedAt}, " +
                      $"last active {connection.LastActivityAt}, " +
                      $"from {connection.RemoteEndPoint}");
}
```

## IO Element Resolution

The `Teltonika.Avl.Elements` package translates raw IO property bytes into meaningful, typed values using a catalog of 150+ element definitions.

### Resolving a single property

```csharp
using Teltonika.Avl.Elements;
using Teltonika.Avl.Models;

var property = new IoProperty(66, new byte[] { 0x2E, 0xE0 });

if (IoElementResolver.TryResolve(property, out var resolved))
{
    Console.WriteLine($"{resolved.Name}: {resolved.Value} {resolved.Units}");
    // Output: External Voltage: 12000 mV
}
```

### Resolving all properties from a record

```csharp
AvlPacket packet = AvlParser.Parse(raw);

foreach (var record in packet.Records)
{
    var properties = IoElementResolver.ResolveAll(record.IoData);

    foreach (var prop in properties)
    {
        Console.WriteLine($"[{prop.Group}] {prop.Name}: {prop.Value} {prop.Units}");
    }
}
```

### Model-aware resolution

Some tracker models use different definitions for the same AVL ID. Pass a `TrackerModel` to get the correct interpretation:

```csharp
using Teltonika.Avl.Elements.Models;

var property = new IoProperty(389, new byte[] { 18 });

// Without model: resolves as "OBD Fuel Type"
var generic = IoElementResolver.Resolve(property);
Console.WriteLine($"{generic?.Name}: {generic?.Value}");

// With model: resolves as "Button Click" on TMT250
var specific = IoElementResolver.Resolve(property, TrackerModel.TMT250);
Console.WriteLine($"{specific?.Name}: {specific?.Value}");
// Output: Button Click: Alarm Button Single Click
```

### Checking element support

```csharp
bool supported = IoElementResolver.IsSupported(66, TrackerModel.FMB920);

IoElementDefinition? def = IoElementResolver.GetDefinition(66);
Console.WriteLine($"ID {def?.Id}: {def?.Name} ({def?.DataType}, {def?.ValueSize} bytes)");
```

### Value types

The resolver returns typed values based on the element definition:

| Data Type | C# Type | Example |
|---|---|---|
| Unsigned | `long` (or `double` with multiplier) | External Voltage: `12000` |
| Signed | `long` (or `double` with multiplier) | Dallas Temperature: `-12.5` |
| Boolean | `bool` | Ignition: `true` |
| Enum | `string` (label) | Green Driving Type: `"Harsh Braking"` |
| Hex | `string` (colon-separated) | iButton: `"01:23:45:67:89:AB:CD:EF"` |
| Ascii | `string` | VIN: `"WVWZZZ3CZWE123456"` |

## Beacon Parsing

Devices with Bluetooth® report nearby BLE beacons through two variable-length IO elements (Codec 8 Extended): AVL ID 385 in simple beacon mode and AVL ID 548 in advanced beacon mode. `BeaconParser` decodes both.

### Simple mode (AVL ID 385)

```csharp
using Teltonika.Avl.Elements.Beacons;

AvlPacket packet = AvlParser.Parse(raw);

foreach (var record in packet.Records)
{
    BeaconList? list = BeaconParser.GetBeacons(record.IoData);
    if (list is null)
        continue; // record carries no beacon list

    Console.WriteLine($"Part {list.CurrentPart}/{list.TotalParts}, {list.Beacons.Count} beacons");

    foreach (var beacon in list.Beacons)
    {
        switch (beacon)
        {
            case IBeacon ib:
                Console.WriteLine($"  iBeacon {ib.Uuid} major={ib.Major} minor={ib.Minor} rssi={ib.Rssi} dBm");
                break;
            case Eddystone es:
                Console.WriteLine($"  Eddystone ns={Convert.ToHexString(es.Namespace.Span)} " +
                                  $"instance={Convert.ToHexString(es.InstanceId.Span)} rssi={es.Rssi} dBm");
                break;
        }

        // EYE beacons can also report battery voltage (mV) and temperature (°C)
        if (beacon.BatteryVoltage is { } mv)
            Console.WriteLine($"    battery: {mv} mV, temperature: {beacon.Temperature} °C");
    }
}
```

The raw payload of a single property can also be parsed directly with `BeaconParser.ParseBeaconList(property.Value)` or the non-throwing `TryParseBeaconList`.

### Advanced mode (AVL ID 548)

In advanced mode the beacon ID and additional data layout follow the device's *Beacon Capturing Configuration*, so they are exposed as raw bytes:

```csharp
IReadOnlyList<AdvancedBeacon>? beacons = BeaconParser.GetAdvancedBeacons(record.IoData);

foreach (var beacon in beacons ?? [])
{
    Console.WriteLine($"RSSI {beacon.Rssi} dBm, ID {Convert.ToHexString(beacon.BeaconId.Span)}, " +
                      $"{beacon.AdditionalData.Length} bytes additional data");
}
```

## Supported Tracker Models

78 models across all Teltonika product lines:

- **FM legacy** &mdash; FM1100, FM1110, FM1120, FM1125, FM1200, FM1202, FM1204, FM2100, FM2200, FM3001, FM3200, FM3300, FM3400, FM3612, FM3622, FM3632, FM4100, FM4200, FM5300, FM5500, FM6300, FM6320
- **FMB** &mdash; FMB001, FMB003, FMB010, FMB020, FMB110, FMB120, FMB125, FMB130, FMB140, FMB150, FMB202, FMB204, FMB208, FMB209, FMB230, FMB640, FMB641, FMB900, FMB910, FMB920, FMB930, FMB940, FMB950, FMB962, FMB964, FMB965
- **FMC** &mdash; FMC001, FMC003, FMC125, FMC130, FMC150, FMC230, FMC640, FMC650, FMC880
- **FMM** &mdash; FMM001, FMM003, FMM125, FMM130, FMM150, FMM230, FMM640, FMM650, FMM800, FMM880
- **FMU** &mdash; FMU125, FMU126, FMU130
- **Other** &mdash; FMT100, TAT100, TAT140, TAT240, TST100, TMT250, GH5200, TFT100

## Benchmarks

Run the [BenchmarkDotNet](https://benchmarkdotnet.org/) performance suite:

```bash
# Run all benchmarks
dotnet run --project benchmarks/Teltonika.Avl.Benchmarks -c Release

# Filter by class
dotnet run --project benchmarks/Teltonika.Avl.Benchmarks -c Release -- --filter "*Parser*"
```

## CRC-16

The library includes a `Crc16Ibm` implementation (polynomial 0xA001) used by the Teltonika protocol, exposed as a public API:

```csharp
using Teltonika.Avl.Protocol;

ushort crc = Crc16Ibm.Compute(dataBytes);
```

## Requirements

- .NET 10.0+

## License

See [LICENSE](LICENSE) for details.
