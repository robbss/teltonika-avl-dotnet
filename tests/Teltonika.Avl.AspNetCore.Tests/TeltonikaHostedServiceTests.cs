using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Teltonika.Avl.AspNetCore;
using Teltonika.Avl.Models;
using Teltonika.Avl.Server;

namespace Teltonika.Avl.AspNetCore.Tests;

public class TeltonikaHostedServiceTests : IAsyncDisposable
{
    private IHost? _host;

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task<IHost> StartHostAsync(int port, Action<IServiceCollection>? configureServices = null)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddTeltonikaServer(o =>
                {
                    o.Address = IPAddress.Loopback;
                    o.Port = port;
                    o.IdleTimeout = TimeSpan.FromSeconds(5);
                });
                configureServices?.Invoke(services);
            });

        _host = builder.Build();
        await _host.StartAsync();

        await Task.Delay(100);

        return _host;
    }

    [Fact]
    public async Task Handler_ReceivesDeviceConnected_AndAvlData()
    {
        int port = GetFreePort();
        var handler = new RecordingHandler();

        var host = await StartHostAsync(port, services =>
        {
            services.AddSingleton(handler);
            services.AddSingleton<ITeltonikaDeviceHandler>(sp => sp.GetRequiredService<RecordingHandler>());
        });

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        await stream.WriteAsync(HexToBytes(ImeiHex));
        var response = new byte[1];
        await stream.ReadExactlyAsync(response);
        Assert.Equal(0x01, response[0]);

        await WaitForAsync(() => handler.ConnectedImeis.Count > 0);
        Assert.Contains("356307042441013", handler.ConnectedImeis);

        await stream.WriteAsync(BuildPacket(Codec8DataField));
        var ack = new byte[4];
        await stream.ReadExactlyAsync(ack);
        int ackCount = (ack[0] << 24) | (ack[1] << 16) | (ack[2] << 8) | ack[3];
        Assert.Equal(1, ackCount);

        await WaitForAsync(() => handler.ReceivedPackets.Count > 0);
        Assert.Single(handler.ReceivedPackets);
        Assert.Equal("356307042441013", handler.ReceivedPackets[0].Imei);
        Assert.Equal(CodecId.Codec8, handler.ReceivedPackets[0].Packet.CodecId);
    }

    [Fact]
    public async Task Handler_ReceivesDeviceDisconnected()
    {
        int port = GetFreePort();
        var handler = new RecordingHandler();

        var host = await StartHostAsync(port, services =>
        {
            services.AddSingleton(handler);
            services.AddSingleton<ITeltonikaDeviceHandler>(sp => sp.GetRequiredService<RecordingHandler>());
        });

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(IPAddress.Loopback, port);
            var stream = client.GetStream();

            await stream.WriteAsync(HexToBytes(ImeiHex));
            var response = new byte[1];
            await stream.ReadExactlyAsync(response);

            await WaitForAsync(() => handler.ConnectedImeis.Count > 0);
        }

        await WaitForAsync(() => handler.DisconnectedImeis.Count > 0);
        Assert.Contains("356307042441013", handler.DisconnectedImeis);
    }

    [Fact]
    public async Task ImeiValidation_HandlerRejects_ServerSendsZero()
    {
        int port = GetFreePort();

        var host = await StartHostAsync(port, services =>
        {
            services.AddTeltonikaHandler<RejectingHandler>();
        });

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        await stream.WriteAsync(HexToBytes(ImeiHex));
        var response = new byte[1];
        await stream.ReadExactlyAsync(response);
        Assert.Equal(0x00, response[0]);
    }

    [Fact]
    public async Task ServerAccessor_ExposesRunningServer()
    {
        int port = GetFreePort();
        var host = await StartHostAsync(port);

        var accessor = host.Services.GetRequiredService<ITeltonikaServerAccessor>();
        Assert.NotNull(accessor.Server);
    }

    [Fact]
    public async Task MultipleHandlers_AllInvoked()
    {
        int port = GetFreePort();
        var handler1 = new RecordingHandler();
        var handler2 = new RecordingHandler();

        var host = await StartHostAsync(port, services =>
        {
            services.AddSingleton<ITeltonikaDeviceHandler>(handler1);
            services.AddSingleton<ITeltonikaDeviceHandler>(handler2);
        });

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();

        await stream.WriteAsync(HexToBytes(ImeiHex));
        var response = new byte[1];
        await stream.ReadExactlyAsync(response);
        Assert.Equal(0x01, response[0]);

        await WaitForAsync(() => handler1.ConnectedImeis.Count > 0);
        await WaitForAsync(() => handler2.ConnectedImeis.Count > 0);

        Assert.Contains("356307042441013", handler1.ConnectedImeis);
        Assert.Contains("356307042441013", handler2.ConnectedImeis);
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            await Task.Delay(50);

        if (!condition())
            throw new TimeoutException("Condition not met within timeout");
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    #region Test helpers

    private sealed class RecordingHandler : ITeltonikaDeviceHandler
    {
        public List<string> ConnectedImeis { get; } = [];
        public List<(string Imei, AvlPacket Packet)> ReceivedPackets { get; } = [];
        public List<string> DisconnectedImeis { get; } = [];

        public Task OnDeviceConnectedAsync(DeviceConnectedEventArgs e, CancellationToken ct)
        {
            ConnectedImeis.Add(e.Imei);
            return Task.CompletedTask;
        }

        public Task OnAvlDataReceivedAsync(AvlDataReceivedEventArgs e, CancellationToken ct)
        {
            ReceivedPackets.Add((e.Imei, e.Packet));
            return Task.CompletedTask;
        }

        public Task OnDeviceDisconnectedAsync(DeviceDisconnectedEventArgs e, CancellationToken ct)
        {
            DisconnectedImeis.Add(e.Imei);
            return Task.CompletedTask;
        }
    }

    private sealed class RejectingHandler : ITeltonikaDeviceHandler
    {
        public ValueTask<bool> ValidateImeiAsync(string imei, CancellationToken ct) => new(false);
    }

    private static byte[] HexToBytes(string hex)
    {
        hex = hex.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    private static byte[] BuildPacket(string dataFieldHex)
    {
        var dataBytes = HexToBytes(dataFieldHex);
        ushort crc = Teltonika.Avl.Protocol.Crc16Ibm.Compute(dataBytes);

        var packet = new byte[4 + 4 + dataBytes.Length + 4];
        int len = dataBytes.Length;
        packet[4] = (byte)(len >> 24);
        packet[5] = (byte)(len >> 16);
        packet[6] = (byte)(len >> 8);
        packet[7] = (byte)len;
        dataBytes.CopyTo(packet, 8);
        int crcOffset = 8 + dataBytes.Length;
        packet[crcOffset + 2] = (byte)(crc >> 8);
        packet[crcOffset + 3] = (byte)crc;

        return packet;
    }

    private const string ImeiHex =
        "000F" + "333536333037303432343431303133";

    private const string Codec8DataField =
        "08" + "01" +
        "0000016B40D8EA30" + "00" +
        "0F0E9D60" + "209A7400" +
        "0000" + "015E" + "0C" + "0000" +
        "01" + "09" +
        "01" + "01" + "01" +
        "02" + "02" + "0001" + "03" + "0003" +
        "03" + "04" + "00000001" + "05" + "00000002" + "06" + "00000003" +
        "03" + "07" + "0000000000000001" + "08" + "0000000000000002" + "09" + "0000000000000003" +
        "01";

    #endregion
}
