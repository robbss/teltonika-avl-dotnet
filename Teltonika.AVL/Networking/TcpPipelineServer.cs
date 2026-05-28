using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Teltonika.AVL.Models;
using Teltonika.AVL.Protocol;

namespace Teltonika.AVL.Networking;

public class TcpPipelineServer(
    int port,
    IEnumerable<ICodecParser> parsers,
    IEnumerable<IPacketHandler> handlers)
{
    private readonly int _port = port;
    private readonly ICodecParser[] _parsers = [.. parsers];
    private readonly IPacketHandler[] _handlers = [.. handlers];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);

                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                _ = ProcessClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using var stream = client.GetStream();

        var reader = PipeReader.Create(stream);
        var writer = PipeWriter.Create(stream);

        try
        {
            var imei = await ProcessHandshakeAsync(reader, writer, cancellationToken);
            if (imei == null)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                while (TryParsePacket(ref buffer, out var packetData, out var crc))
                {
                    if (packetData.Length > 0)
                    {
                        var dataSpan = packetData.IsSingleSegment
                            ? packetData.FirstSpan
                            : packetData.ToArray();

                        var calculatedCrc = Crc16.Compute(dataSpan);
                        if (calculatedCrc != crc)
                        {
                            return;
                        }

                        var codecId = (AvlCodec)dataSpan[0];
                        var parser = _parsers.FirstOrDefault(p => p.CodecId == codecId);

                        var recordCount = 0;
                        if (parser != null && parser.TryParse(dataSpan, out var parsedPacket) && parsedPacket != null)
                        {
                            recordCount = parsedPacket.RecordCount;

                            foreach (var handler in _handlers)
                            {
                                await handler.HandlePacketAsync(imei, parsedPacket, cancellationToken);
                            }
                        }

                        var ackBuffer = writer.GetMemory(4);
                        BinaryPrimitives.WriteInt32BigEndian(ackBuffer.Span, recordCount);
                        writer.Advance(4);
                        await writer.FlushAsync(cancellationToken);
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }
        finally
        {
            await reader.CompleteAsync();
            await writer.CompleteAsync();
            client.Close();
        }
    }

    private static async Task<string?> ProcessHandshakeAsync(PipeReader reader, PipeWriter writer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await reader.ReadAsync(cancellationToken);
            var buffer = result.Buffer;

            if (buffer.Length >= 2)
            {
                var lengthBuffer = buffer.Slice(0, 2);
                Span<byte> lengthSpan = new byte[2];
                lengthBuffer.CopyTo(lengthSpan);
                var imeiLength = BinaryPrimitives.ReadInt16BigEndian(lengthSpan);

                if (buffer.Length >= 2 + imeiLength)
                {
                    var imeiBuffer = buffer.Slice(2, imeiLength);
                    Span<byte> imeiSpan = new byte[imeiLength];
                    imeiBuffer.CopyTo(imeiSpan);
                    var imei = Encoding.ASCII.GetString(imeiSpan);

                    reader.AdvanceTo(buffer.GetPosition(2 + imeiLength));

                    writer.GetSpan(1)[0] = 0x01;
                    writer.Advance(1);
                    await writer.FlushAsync(cancellationToken);

                    return imei;
                }
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }

        return null;
    }

    private static bool TryParsePacket(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> packetData, out ushort crc)
    {
        packetData = default;
        crc = 0;

        if (buffer.Length < 8)
        {
            return false;
        }

        Span<byte> header = new byte[8];
        buffer.Slice(0, 8).CopyTo(header);

        var preamble = BinaryPrimitives.ReadInt32BigEndian(header[..4]);
        if (preamble != 0)
        {
            throw new InvalidOperationException("Invalid packet preamble.");
        }

        var dataLength = BinaryPrimitives.ReadInt32BigEndian(header.Slice(4, 4));
        var totalPacketLength = 8 + dataLength + 4;

        if (buffer.Length < totalPacketLength)
        {
            return false;
        }

        packetData = buffer.Slice(8, dataLength);

        Span<byte> crcSpan = new byte[4];
        buffer.Slice(8 + dataLength, 4).CopyTo(crcSpan);
        crc = (ushort)BinaryPrimitives.ReadInt32BigEndian(crcSpan);

        buffer = buffer.Slice(totalPacketLength);

        return true;
    }
}