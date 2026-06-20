using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Teltonika.Avl.Codecs;
using Teltonika.Avl.Models;

namespace Teltonika.Avl.Server;

public delegate Task AsyncEventHandler<in TEventArgs>(object sender, TEventArgs e);

public sealed class TeltonikaServer : IAsyncDisposable
{
    private readonly TeltonikaServerOptions _options;
    private readonly ConcurrentDictionary<string, DeviceConnection> _connections = new();
    private readonly SemaphoreSlim _connectionGate;
    private Socket? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;

    public event AsyncEventHandler<DeviceConnectedEventArgs>? DeviceConnected;
    public event AsyncEventHandler<AvlDataReceivedEventArgs>? AvlDataReceived;
    public event AsyncEventHandler<DeviceDisconnectedEventArgs>? DeviceDisconnected;
    public event AsyncEventHandler<CommandReceivedEventArgs>? CommandReceived;

    public IReadOnlyDictionary<string, DeviceConnection> Connections => _connections;

    public TeltonikaServer(TeltonikaServerOptions options)
    {
        _options = options;
        _connectionGate = new SemaphoreSlim(options.MaxConnections, options.MaxConnections);
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Bind(new IPEndPoint(_options.Address, _options.Port));
        _listener.Listen(_options.MaxConnections);

        _acceptTask = AcceptLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is not null)
            await _cts.CancelAsync();

        _listener?.Close();

        if (_acceptTask is not null)
        {
            try { await _acceptTask; }
            catch (OperationCanceledException) { }
        }

        var connections = _connections.Values.ToArray();
        foreach (var conn in connections)
        {
            _connections.TryRemove(conn.Imei, out _);
            await conn.DisposeAsync();
        }
    }

    public async ValueTask SendCommandAsync(string imei, string command, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(imei, out var connection))
            throw new InvalidOperationException($"Device {imei} is not connected");

        var packet = Codec12Encoder.Instance.EncodeCommandPacket(
            new Models.GprsCommandPacket(Models.CodecId.Codec12, 0x05, command));
        await connection.WriteAsync(packet, ct);
    }

    public async ValueTask SendCommandCodec14Async(string imei, string command, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(imei, out var connection))
            throw new InvalidOperationException($"Device {imei} is not connected");

        var packet = Codec14Encoder.Instance.EncodeCommandPacket(
            new Models.GprsCommandPacket(Models.CodecId.Codec14, 0x05, command, Imei: imei));
        await connection.WriteAsync(packet, ct);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _connectionGate.WaitAsync(ct);
                var socket = await _listener!.AcceptAsync(ct);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                _ = HandleConnectionAsync(socket, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private async Task HandleConnectionAsync(Socket socket, CancellationToken ct)
    {
        var connection = new DeviceConnection(socket);
        string? imei = null;
        string disconnectReason = "closed";

        try
        {
            imei = await connection.PerformImeiHandshakeAsync(_options.ImeiValidator, ct);
            if (imei is null)
            {
                disconnectReason = "rejected";
                return;
            }

            _connections[imei] = connection;

            await InvokeAsync(DeviceConnected, new DeviceConnectedEventArgs
            {
                Imei = imei,
                RemoteEndPoint = connection.RemoteEndPoint,
                ConnectedAt = connection.ConnectedAt
            });

            await connection.RunDataLoopAsync(
                async packet => await InvokeAsync(AvlDataReceived, new AvlDataReceivedEventArgs
                {
                    Imei = imei,
                    Packet = packet
                }),
                async command => await InvokeAsync(CommandReceived, new CommandReceivedEventArgs
                {
                    Imei = imei,
                    Command = command
                }),
                _options.IdleTimeout,
                ct);
        }
        catch (TimeoutException)
        {
            disconnectReason = "idle timeout";
        }
        catch (IOException)
        {
            disconnectReason = "connection lost";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            disconnectReason = "server shutdown";
        }
        catch (Exception ex)
        {
            disconnectReason = $"error: {ex.Message}";
        }
        finally
        {
            if (imei is not null)
            {
                _connections.TryRemove(imei, out _);
                await InvokeAsync(DeviceDisconnected, new DeviceDisconnectedEventArgs
                {
                    Imei = imei,
                    Reason = disconnectReason
                });
            }

            await connection.DisposeAsync();
            _connectionGate.Release();
        }
    }

    private async Task InvokeAsync<T>(AsyncEventHandler<T>? handler, T args)
    {
        if (handler is null)
            return;

        foreach (var d in handler.GetInvocationList().Cast<AsyncEventHandler<T>>())
            await d(this, args);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _connectionGate.Dispose();
        _cts?.Dispose();
    }
}
