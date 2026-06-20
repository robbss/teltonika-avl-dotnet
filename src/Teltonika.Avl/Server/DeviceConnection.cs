using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Teltonika.Avl.Codecs;
using Teltonika.Avl.Models;
using Teltonika.Avl.Protocol;

namespace Teltonika.Avl.Server;

public sealed class DeviceConnection : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly PipeReader _pipeReader;
    private readonly PipeWriter _pipeWriter;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    public string Imei { get; internal set; } = string.Empty;
    public IPEndPoint RemoteEndPoint { get; }
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; internal set; } = DateTimeOffset.UtcNow;

    internal DeviceConnection(Socket socket)
    {
        _socket = socket;
        RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint!;
        var stream = new NetworkStream(socket, ownsSocket: false);
        _pipeReader = PipeReader.Create(stream);
        _pipeWriter = PipeWriter.Create(stream);
    }

    internal async Task<string?> PerformImeiHandshakeAsync(
        Func<string, CancellationToken, ValueTask<bool>>? validator,
        CancellationToken ct)
    {
        while (true)
        {
            var result = await _pipeReader.ReadAsync(ct);
            var buffer = result.Buffer;

            if (ImeiReader.TryRead(in buffer, out string? imei, out var consumed))
            {
                _pipeReader.AdvanceTo(consumed);

                bool accepted = true;
                if (validator is not null)
                    accepted = await validator(imei!, ct);

                var response = new byte[] { accepted ? (byte)0x01 : (byte)0x00 };
                await WriteAsync(response, ct);

                if (accepted)
                {
                    Imei = imei!;
                    LastActivityAt = DateTimeOffset.UtcNow;
                    return imei;
                }

                return null;
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                return null;
        }
    }

    internal async Task RunDataLoopAsync(
        Func<AvlPacket, Task> onDataReceived,
        Func<GprsCommandPacket, Task> onCommandReceived,
        TimeSpan idleTimeout,
        CancellationToken ct)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        var token = linkedCts.Token;

        while (!token.IsCancellationRequested)
        {
            ReadResult result;
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutCts.CancelAfter(idleTimeout);
                result = await _pipeReader.ReadAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new TimeoutException("Idle timeout exceeded");
            }

            var buffer = result.Buffer;

            while (AvlPacketReader.TryReadPacket(in buffer, out var dataField, out var consumed, out var examined))
            {
                LastActivityAt = DateTimeOffset.UtcNow;

                var reader = new SequenceReader<byte>(dataField);
                if (!reader.TryPeek(out byte codecByte))
                    break;

                if (CodecDecoderFactory.IsDataCodec(codecByte))
                {
                    var decoder = CodecDecoderFactory.GetDataDecoder((CodecId)codecByte);
                    var packet = decoder.DecodeDataPacket(ref reader);

                    int recordCount = packet.Records.Count;
                    var ack = new byte[4];
                    ack[0] = (byte)(recordCount >> 24);
                    ack[1] = (byte)(recordCount >> 16);
                    ack[2] = (byte)(recordCount >> 8);
                    ack[3] = (byte)recordCount;
                    await WriteAsync(ack, token);

                    await onDataReceived(packet);
                }
                else if (CodecDecoderFactory.IsCommandCodec(codecByte))
                {
                    var decoder = CodecDecoderFactory.GetCommandDecoder((CodecId)codecByte);
                    var command = decoder.DecodeCommandPacket(ref reader);
                    await onCommandReceived(command);
                }

                buffer = buffer.Slice(consumed);
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                break;
        }
    }

    internal async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var result = await _pipeWriter.WriteAsync(data, ct);
            if (result.IsCompleted)
                throw new IOException("Connection closed during write");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await _pipeReader.CompleteAsync();
        await _pipeWriter.CompleteAsync();
        _socket.Close();
        _socket.Dispose();
        _writeLock.Dispose();
        _cts.Dispose();
    }
}
