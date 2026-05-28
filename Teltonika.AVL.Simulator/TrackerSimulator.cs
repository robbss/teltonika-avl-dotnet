using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Teltonika.AVL.Models;
using Teltonika.AVL.Protocol;

namespace Teltonika.AVL.Simulator;

public class TrackerSimulator
{
    private readonly string _imei;
    private readonly string _host;
    private readonly int _port;
    private readonly ICodecEncoder _encoder;

    public TrackerSimulator(string imei, string host, int port)
    {
        _imei = imei;
        _host = host;
        _port = port;
        _encoder = new Codec8ExtendedEncoder();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(_host, _port, cancellationToken);
            await using var stream = client.GetStream();

            // Handshake
            var imeiBytes = Encoding.ASCII.GetBytes(_imei);
            var handshake = new byte[2 + imeiBytes.Length];
            BinaryPrimitives.WriteUInt16BigEndian(handshake.AsSpan(0, 2), (ushort)imeiBytes.Length);
            imeiBytes.CopyTo(handshake, 2);

            await stream.WriteAsync(handshake, cancellationToken);

            var response = new byte[1];
            await stream.ReadExactlyAsync(response, cancellationToken);

            if (response[0] == 0x01)
            {
                Console.WriteLine($"[{_imei}] Handshake successful.");
            }
            else
            {
                Console.WriteLine($"[{_imei}] Handshake rejected.");
                return;
            }

            var rnd = new Random();
            double lat = 54.6872;
            double lon = 25.2797;

            while (!cancellationToken.IsCancellationRequested)
            {
                lat += (rnd.NextDouble() - 0.5) * 0.001;
                lon += (rnd.NextDouble() - 0.5) * 0.001;

                var extVoltageBytes = new byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(extVoltageBytes, (uint)rnd.Next(11000, 14000));

                var packet = new AvlDataPacket
                {
                    CodecId = AvlCodec.Codec8Extended,
                    RecordCount = 1,
                    Records = new List<AvlRecord>
                    {
                        new AvlRecord
                        {
                            Timestamp = DateTimeOffset.UtcNow,
                            Priority = 1,
                            Gps = new GpsElement
                            {
                                Latitude = lat,
                                Longitude = lon,
                                Altitude = 120,
                                Angle = (short)rnd.Next(0, 360),
                                Satellites = 12,
                                Speed = (short)rnd.Next(0, 100)
                            },
                            Io = new RawIoElement
                            {
                                EventId = 0,
                                TotalIoCount = 2,
                                Properties = new Dictionary<int, byte[]>
                                {
                                    { 239, new byte[] { 1 } }, // Ignition = true
                                    { 66, extVoltageBytes }    // External Voltage
                                }
                            }
                        }
                    }
                };

                var payload = _encoder.Encode(packet);
                await stream.WriteAsync(payload, cancellationToken);

                var ackBuffer = new byte[4];
                await stream.ReadExactlyAsync(ackBuffer, cancellationToken);
                var ackCount = BinaryPrimitives.ReadInt32BigEndian(ackBuffer);

                Console.WriteLine($"[{_imei}] Sent 1 record. Server ACK: {ackCount}");

                await Task.Delay(5000, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"[{_imei}] Connection error: {ex.Message}");
        }
    }
}