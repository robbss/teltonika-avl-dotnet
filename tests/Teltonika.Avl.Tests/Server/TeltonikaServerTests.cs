using System.Net;
using System.Net.Sockets;
using Teltonika.Avl.Models;
using Teltonika.Avl.Server;
using Teltonika.Avl.Tests.TestData;

namespace Teltonika.Avl.Tests.Server;

public class TeltonikaServerTests : IAsyncDisposable
{
    private TeltonikaServer? _server;

    private async Task<TeltonikaServer> StartServerAsync(int port, Func<string, CancellationToken, ValueTask<bool>>? validator = null)
    {
        var options = new TeltonikaServerOptions
        {
            Address = IPAddress.Loopback,
            Port = port,
            IdleTimeout = TimeSpan.FromSeconds(5),
            ImeiValidator = validator
        };
        _server = new TeltonikaServer(options);
        await _server.StartAsync();
        return _server;
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public async Task FullFlow_ImeiHandshake_DataReceived_AckSent()
    {
        int port = GetFreePort();
        var server = await StartServerAsync(port);

        var dataReceived = new TaskCompletionSource<AvlDataReceivedEventArgs>();
        var deviceConnected = new TaskCompletionSource<DeviceConnectedEventArgs>();

        server.DeviceConnected += (_, e) => { deviceConnected.TrySetResult(e); return Task.CompletedTask; };
        server.AvlDataReceived += (_, e) => { dataReceived.TrySetResult(e); return Task.CompletedTask; };

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        // Send IMEI
        var imeiBytes = SamplePackets.HexToBytes(SamplePackets.ImeiHex);
        await stream.WriteAsync(imeiBytes);

        // Read IMEI acceptance
        var response = new byte[1];
        await stream.ReadExactlyAsync(response);
        Assert.Equal(0x01, response[0]);

        // Verify DeviceConnected event
        var connArgs = await WithTimeout(deviceConnected.Task);
        Assert.Equal("356307042441013", connArgs.Imei);

        // Send AVL data packet
        var avlPacket = SamplePackets.BuildPacket(SamplePackets.Codec8DataField);
        await stream.WriteAsync(avlPacket);

        // Read ACK (4 bytes = record count)
        var ack = new byte[4];
        await stream.ReadExactlyAsync(ack);
        int ackCount = (ack[0] << 24) | (ack[1] << 16) | (ack[2] << 8) | ack[3];
        Assert.Equal(1, ackCount);

        // Verify data received event
        var dataArgs = await WithTimeout(dataReceived.Task);
        Assert.Equal("356307042441013", dataArgs.Imei);
        Assert.Equal(CodecId.Codec8, dataArgs.Packet.CodecId);
        Assert.Single(dataArgs.Packet.Records);
    }

    [Fact]
    public async Task ImeiRejection_ServerSendsZero_DisconnectsDevice()
    {
        int port = GetFreePort();
        var server = await StartServerAsync(port, (_, _) => new ValueTask<bool>(false));

        var disconnected = new TaskCompletionSource<DeviceDisconnectedEventArgs>();
        server.DeviceDisconnected += (_, e) => { disconnected.TrySetResult(e); return Task.CompletedTask; };

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        // Send IMEI
        var imeiBytes = SamplePackets.HexToBytes(SamplePackets.ImeiHex);
        await stream.WriteAsync(imeiBytes);

        // Read rejection
        var response = new byte[1];
        await stream.ReadExactlyAsync(response);
        Assert.Equal(0x00, response[0]);
    }

    [Fact]
    public async Task SendCommand_DeviceReceivesCodec12Frame()
    {
        int port = GetFreePort();
        var server = await StartServerAsync(port);

        var deviceConnected = new TaskCompletionSource<DeviceConnectedEventArgs>();
        server.DeviceConnected += (_, e) => { deviceConnected.TrySetResult(e); return Task.CompletedTask; };

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        // IMEI handshake
        await stream.WriteAsync(SamplePackets.HexToBytes(SamplePackets.ImeiHex));
        var response = new byte[1];
        await stream.ReadExactlyAsync(response);
        Assert.Equal(0x01, response[0]);

        await WithTimeout(deviceConnected.Task);

        // Send command from server
        await server.SendCommandAsync("356307042441013", "getinfo");

        // Read the command packet on the client side
        var header = new byte[8];
        await stream.ReadExactlyAsync(header);
        int dataLen = (header[4] << 24) | (header[5] << 16) | (header[6] << 8) | header[7];

        var rest = new byte[dataLen + 4]; // data + CRC
        await stream.ReadExactlyAsync(rest);

        // Verify it's a valid Codec 12 packet
        var fullPacket = new byte[8 + dataLen + 4];
        header.CopyTo(fullPacket, 0);
        rest.CopyTo(fullPacket, 8);

        var parsed = AvlParser.ParseCommand(fullPacket);
        Assert.Equal(CodecId.Codec12, parsed.CodecId);
        Assert.Equal("getinfo", parsed.CommandText);
        Assert.Equal(0x05, parsed.CommandType);
    }

    [Fact]
    public async Task Connections_TracksActiveDevices()
    {
        int port = GetFreePort();
        var server = await StartServerAsync(port);
        var deviceConnected = new TaskCompletionSource<DeviceConnectedEventArgs>();
        server.DeviceConnected += (_, e) => { deviceConnected.TrySetResult(e); return Task.CompletedTask; };

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        await stream.WriteAsync(SamplePackets.HexToBytes(SamplePackets.ImeiHex));
        var response = new byte[1];
        await stream.ReadExactlyAsync(response);

        await WithTimeout(deviceConnected.Task);

        Assert.True(server.Connections.ContainsKey("356307042441013"));
    }

    private static async Task<T> WithTimeout<T>(Task<T> task, int ms = 5000)
    {
        var completed = await Task.WhenAny(task, Task.Delay(ms));
        if (completed != task)
            throw new TimeoutException("Test timed out waiting for event");
        return await task;
    }

    public async ValueTask DisposeAsync()
    {
        if (_server is not null)
            await _server.DisposeAsync();
    }
}
