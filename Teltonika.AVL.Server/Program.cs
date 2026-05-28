using Teltonika.AVL.AspNetCore.Extensions;
using Teltonika.AVL.Models;
using Teltonika.AVL.Networking;
using Teltonika.AVL.Profiles;
using Teltonika.AVL.Protocol;

var builder = WebApplication.CreateBuilder(args);

// Add the TCP Server to DI, listening on port 1234
builder.Services.AddTeltonikaTcpServer(1234);

// Register a dummy IPacketHandler to log incoming AVL packets
builder.Services.AddSingleton<IPacketHandler, ConsoleLoggerPacketHandler>();

var app = builder.Build();

app.MapGet("/", () => "Teltonika AVL Server is running and listening on TCP port 1234.");

app.Run();

file class ConsoleLoggerPacketHandler(ILogger<ConsoleLoggerPacketHandler> logger) : IPacketHandler
{
    private readonly ILogger<ConsoleLoggerPacketHandler> _logger = logger;

    public Task HandlePacketAsync(string imei, AvlDataPacket packet, CancellationToken cancellationToken)
    {
#pragma warning disable CA1873 // Avoid potentially expensive logging
        _logger.LogInformation("Received packet from IMEI: {Imei}. Codec: {Codec}, Records: {Count}",
            imei, packet.CodecId, packet.RecordCount);
#pragma warning restore CA1873 // Avoid potentially expensive logging

        var elementParser = new IoElementsParser();
        var profile = new Fmc234Profile();

        foreach (var record in packet.Records)
        {
#pragma warning disable CA1873 // Avoid potentially expensive logging
            _logger.LogInformation("  Record Timestamp: {Timestamp}, GPS: Lat {Lat}, Lng {Lng}, Speed {Speed} km/h",
                record.Timestamp, record.Gps.Latitude, record.Gps.Longitude, record.Gps.Speed);
#pragma warning restore CA1873 // Avoid potentially expensive logging

            if (record.Io != null)
            {
                var elements = elementParser.Parse(record.Io, profile);

#pragma warning disable CA1873 // Avoid potentially expensive logging
                _logger.LogInformation("    Raw IO Count: {IoCount}", record.Io.Properties.Count);
#pragma warning restore CA1873 // Avoid potentially expensive logging
                foreach (var ioProp in record.Io.Properties)
                {
#pragma warning disable CA1873 // Avoid potentially expensive logging
                    _logger.LogInformation("      IO ID: {Id}, Value length: {Length} bytes", ioProp.Key, ioProp.Value.Length);
#pragma warning restore CA1873 // Avoid potentially expensive logging
                }
            }
        }
        return Task.CompletedTask;
    }
}