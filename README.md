# Teltonika AVL

A .NET 10 library for parsing, encoding, and serving [Teltonika](https://teltonika-gps.com/) GPS tracker data using the AVL protocol. Zero external dependencies for the core library.

## Features

- **Full codec support** &mdash; Codec 8, Codec 8 Extended, Codec 16 (data), Codec 12/13/14 (commands)
- **Parse & encode** &mdash; Symmetric API for reading and writing AVL packets
- **TCP server** &mdash; Production-ready async server with IMEI validation, idle timeouts, and bidirectional GPRS commands
- **IO element resolution** &mdash; Translate raw IO property IDs into named, typed values with units, using a built-in catalog of 150+ definitions across 78 tracker models
- **Per-model overrides** &mdash; Handles cases where the same AVL ID has different meanings on different hardware (e.g. ID 389 is "OBD Fuel Type" on FMB devices but "Button Click" on TMT250)
- **Device simulator** &mdash; A CLI tool that simulates a Teltonika tracker for integration testing
- **Zero allocations where possible** &mdash; `ReadOnlySpan<byte>` and `ReadOnlySequence<byte>` APIs, `FrozenDictionary` lookups

## Projects

| Project | Description |
|---|---|
| `Teltonika.Avl` | Core parser, encoder, and TCP server |
| `Teltonika.Avl.Elements` | IO element catalog and property resolver |
| `Teltonika.Simulator` | CLI device simulator for testing |

## Installation

Clone and build from source:

```bash
git clone <repo-url>
cd teltonika-avl-dotnet-new
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

## Supported Tracker Models

78 models across all Teltonika product lines:

- **FM legacy** &mdash; FM1100, FM1110, FM1120, FM1125, FM1200, FM1202, FM1204, FM2100, FM2200, FM3001, FM3200, FM3300, FM3400, FM3612, FM3622, FM3632, FM4100, FM4200, FM5300, FM5500, FM6300, FM6320
- **FMB** &mdash; FMB001, FMB003, FMB010, FMB020, FMB110, FMB120, FMB125, FMB130, FMB140, FMB150, FMB202, FMB204, FMB208, FMB209, FMB230, FMB640, FMB641, FMB900, FMB910, FMB920, FMB930, FMB940, FMB950, FMB962, FMB964, FMB965
- **FMC** &mdash; FMC001, FMC003, FMC125, FMC130, FMC150, FMC230, FMC640, FMC650, FMC880
- **FMM** &mdash; FMM001, FMM003, FMM125, FMM130, FMM150, FMM230, FMM640, FMM650, FMM800, FMM880
- **FMU** &mdash; FMU125, FMU126, FMU130
- **Other** &mdash; FMT100, TAT100, TAT140, TAT240, TST100, TMT250, GH5200, TFT100

## Device Simulator

The `Teltonika.Simulator` project is a CLI tool that simulates a Teltonika tracker, useful for integration testing without physical hardware.

### Random walk mode

```bash
dotnet run --project src/Teltonika.Simulator
```

### Commute profile mode

Simulate realistic daily commutes with route-following. Addresses are geocoded via [Nominatim](https://nominatim.openstreetmap.org/) and routes are planned via [OSRM](http://project-osrm.org/) — no API keys required.

Create a profile JSON file:

```json
{
  "server": {
    "host": "127.0.0.1",
    "port": 5027
  },
  "defaults": {
    "codec": "8e",
    "sendIntervalSeconds": 10,
    "timeAcceleration": 60,
    "drivingSpeedKmh": 50,
    "parkingIntervalSeconds": 300
  },
  "devices": [
    {
      "imei": "356307042441013",
      "homeAddress": "Gedimino pr. 1, Vilnius",
      "workAddress": "Konstitucijos pr. 7, Vilnius",
      "workStartHour": 8.5,
      "workEndHour": 17.0,
      "departureVarianceMinutes": 15
    }
  ]
}
```

Run with the profile:

```bash
dotnet run --project src/Teltonika.Simulator -- --profile commute.json
```

Each device follows a daily cycle: **AtHome** (parked) &rarr; **DrivingToWork** (following real road route) &rarr; **AtWork** (parked) &rarr; **DrivingHome** &rarr; repeat. Time acceleration lets you run an 8-hour workday in ~8 real minutes (`timeAcceleration: 60`). Routes are cached in a `.routes.json` file alongside the profile to avoid repeated API calls.

| Field | Description |
|---|---|
| `timeAcceleration` | 1 real second = N simulated seconds (60 = 1 min/sec) |
| `workStartHour` / `workEndHour` | Decimal hours (8.5 = 08:30) |
| `departureVarianceMinutes` | Random daily jitter on departure times |
| `drivingSpeedKmh` | Base driving speed (±10% random variation) |
| `parkingIntervalSeconds` | How often to send stationary pings (simulated time) |

Multiple devices run in parallel, each on its own TCP connection.

### Programmatic usage

Use the simulator programmatically:

```csharp
using Teltonika.Avl.Models;
using Teltonika.Simulator;

var device = new SimulatedDevice(new SimulatorOptions
{
    Imei = "356307042441013",
    Host = "127.0.0.1",
    Port = 5027,
    DataCodec = CodecId.Codec8Extended,
    SendInterval = TimeSpan.FromSeconds(10),
    InitialLatitude = 54.6993,
    InitialLongitude = 25.2616,
});

await device.ConnectAsync();

// Send a single record with simulated GPS data
var gps = new GpsSimulator(54.6993, 25.2616);
var record = new AvlRecord(
    DateTimeOffset.UtcNow,
    Priority.Low,
    gps.Next(),
    new IoElement(0, Array.Empty<IoProperty>()));

int ack = await device.SendRecordAsync(record);
Console.WriteLine($"Server acknowledged {ack} record(s)");

// Listen for commands from the server
var command = await device.ReceiveCommandAsync();
if (command is not null)
{
    Console.WriteLine($"Received command: {command.CommandText}");
    await device.SendCommandResponseAsync("OK");
}

await device.DisposeAsync();
```

## Docker

The simulator ships with a Dockerfile for containerized deployment.

### Build the image

```bash
docker build -t teltonika-simulator .
```

### Run with environment variables

```bash
# Random walk mode
docker run --rm \
  -e SIMULATOR_HOST=192.168.1.100 \
  -e SIMULATOR_PORT=5027 \
  -e SIMULATOR_IMEI=356307042441013 \
  teltonika-simulator

# Profile mode with a mounted profile
docker run --rm \
  -e SIMULATOR_PROFILE=/profiles/commute.json \
  -e SIMULATOR_HOST=192.168.1.100 \
  -v ./profiles:/profiles \
  teltonika-simulator
```

### Environment variables

All settings can be configured via environment variables. CLI args take priority over env vars.

| Variable | Description | Default |
|---|---|---|
| `SIMULATOR_PROFILE` | Path to commute profile JSON | _(none)_ |
| `SIMULATOR_HOST` | Server host | `127.0.0.1` |
| `SIMULATOR_PORT` | Server port | `5027` |
| `SIMULATOR_IMEI` | Device IMEI | `356307042441013` |
| `SIMULATOR_CODEC` | Codec: `8`, `8e`, `16` | `8e` |
| `SIMULATOR_INTERVAL` | Send interval (seconds) | `10` |
| `SIMULATOR_COUNT` | Packets to send, `0` = infinite | `0` |
| `SIMULATOR_LAT` | Initial latitude | `54.6993` |
| `SIMULATOR_LON` | Initial longitude | `25.2616` |

### Docker Compose

```yaml
services:
  simulator:
    build: .
    environment:
      SIMULATOR_PROFILE: /profiles/commute.json
      SIMULATOR_HOST: host.docker.internal
      SIMULATOR_PORT: "5027"
    volumes:
      - ./profiles:/profiles
```

Route cache files are written alongside the profile JSON, so mounting the profiles directory as a volume preserves cached routes across container restarts.

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
