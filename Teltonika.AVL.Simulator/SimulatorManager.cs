using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Teltonika.AVL.Models;
using Teltonika.AVL.Protocol;
using System.Buffers.Binary;

namespace Teltonika.AVL.Simulator;

public class SimulatorManager
{
    private readonly ConcurrentDictionary<string, TrackerState> _trackers = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsList = new();
    private readonly ConcurrentDictionary<string, List<string>> _logs = new();
    private readonly ICodecEncoder _encoder = new Codec8ExtendedEncoder();

    public string TargetHost { get; set; } = "127.0.0.1";
    public int TargetPort { get; set; } = 1234;

    public SimulatorManager()
    {
        // Add a default pre-configured tracker for demo in Sabro, Denmark
        AddTracker("353344000000001", new List<TrackerInstruction>
        {
            new() { Type = InstructionType.SetIgnition, IgnitionValue = true },
            new() { Type = InstructionType.Wait, DurationSeconds = 5 },
            new() { Type = InstructionType.MoveTo, Latitude = 56.2150, Longitude = 10.0450, Speed = 60 },
            new() { Type = InstructionType.Wait, DurationSeconds = 5 },
            new() { Type = InstructionType.SetIgnition, IgnitionValue = false }
        });
    }

    public List<TrackerState> GetTrackers() => _trackers.Values.ToList();

    public List<string> GetLogs(string imei)
    {
        if (_logs.TryGetValue(imei, out var list))
        {
            lock (list)
            {
                return list.TakeLast(100).ToList();
            }
        }
        return new List<string>();
    }

    public void AddTracker(string imei, List<TrackerInstruction> instructions, double? latitude = null, double? longitude = null)
    {
        var state = new TrackerState
        {
            Imei = imei,
            Instructions = instructions,
            IsRunning = false
        };
        if (latitude.HasValue) state.Latitude = latitude.Value;
        if (longitude.HasValue) state.Longitude = longitude.Value;
        
        _trackers[imei] = state;
        _logs[imei] = new List<string> { "Tracker created." };
    }

    public void RemoveTracker(string imei)
    {
        StopTracker(imei);
        _trackers.TryRemove(imei, out _);
        _logs.TryRemove(imei, out _);
    }

    public void StartTracker(string imei)
    {
        if (!_trackers.TryGetValue(imei, out var state) || state.IsRunning) return;

        state.IsRunning = true;
        state.CurrentInstructionIndex = 0;
        state.InstructionStartTime = null;
        state.RemainingWaitSeconds = null;
        state.InstructionStatus = "Starting...";

        var cts = new CancellationTokenSource();
        _ctsList[imei] = cts;

        _ = RunTrackerLoopAsync(state, cts.Token);
    }

    public void StopTracker(string imei)
    {
        if (_ctsList.TryRemove(imei, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        if (_trackers.TryGetValue(imei, out var state))
        {
            state.IsRunning = false;
            state.InstructionStatus = "Stopped";
            state.Speed = 0;
            AddLog(imei, "Simulator stopped.");
        }
    }

    public void UpdateTracker(string imei, List<TrackerInstruction> instructions)
    {
        if (_trackers.TryGetValue(imei, out var state))
        {
            state.Instructions = instructions;
            AddLog(imei, "Instructions updated.");
        }
    }

    private void AddLog(string imei, string message)
    {
        if (_logs.TryGetValue(imei, out var list))
        {
            lock (list)
            {
                list.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                if (list.Count > 100)
                {
                    list.RemoveAt(0);
                }
            }
        }
    }

    private async Task RunTrackerLoopAsync(TrackerState state, CancellationToken ct)
    {
        var imei = state.Imei;
        AddLog(imei, $"Connecting to {TargetHost}:{TargetPort}...");

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(TargetHost, TargetPort, ct);
            await using var stream = client.GetStream();

            // Handshake
            var imeiBytes = Encoding.ASCII.GetBytes(imei);
            var handshake = new byte[2 + imeiBytes.Length];
            BinaryPrimitives.WriteUInt16BigEndian(handshake.AsSpan(0, 2), (ushort)imeiBytes.Length);
            imeiBytes.CopyTo(handshake, 2);

            await stream.WriteAsync(handshake, ct);
            var response = new byte[1];
            await stream.ReadExactlyAsync(response, ct);

            if (response[0] == 0x01)
            {
                AddLog(imei, "Handshake accepted.");
            }
            else
            {
                AddLog(imei, "Handshake rejected.");
                StopTracker(imei);
                return;
            }

            while (!ct.IsCancellationRequested)
            {
                // Execute current instruction step
                ExecuteStep(state);

                // Send current AVL packet
                var packet = GeneratePacket(state);
                var payload = _encoder.Encode(packet);
                await stream.WriteAsync(payload, ct);

                var ackBuffer = new byte[4];
                await stream.ReadExactlyAsync(ackBuffer, ct);
                var ackCount = BinaryPrimitives.ReadInt32BigEndian(ackBuffer);

                AddLog(imei, $"Sent packet (1 record). ACK: {ackCount}. Lat: {state.Latitude:F5}, Lng: {state.Longitude:F5}, Speed: {state.Speed:F1} km/h, Ignition: {state.Ignition}");

                await Task.Delay(state.UpdateIntervalMs, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AddLog(imei, $"Error: {ex.Message}");
            StopTracker(imei);
        }
    }

    private void ExecuteStep(TrackerState state)
    {
        if (state.Instructions == null || state.Instructions.Count == 0)
        {
            state.InstructionStatus = "Idle (No instructions)";
            state.Speed = 0;
            return;
        }

        if (state.CurrentInstructionIndex >= state.Instructions.Count)
        {
            // Loop or finish
            state.CurrentInstructionIndex = 0;
            AddLog(state.Imei, "Looping instructions sequence back to start.");
        }

        var inst = state.Instructions[state.CurrentInstructionIndex];
        switch (inst.Type)
        {
            case InstructionType.SetIgnition:
                state.Ignition = inst.IgnitionValue ?? false;
                state.Speed = 0;
                state.ProgressPercentage = 100.0;
                state.InstructionStatus = $"Ignition set to {state.Ignition}";
                AddLog(state.Imei, state.InstructionStatus);
                state.CurrentInstructionIndex++;
                break;

            case InstructionType.Wait:
                if (!state.InstructionStartTime.HasValue)
                {
                    state.InstructionStartTime = DateTime.UtcNow;
                    state.RemainingWaitSeconds = inst.DurationSeconds ?? 5;
                    state.InitialWaitSeconds = inst.DurationSeconds ?? 5;
                }

                var elapsed = (DateTime.UtcNow - state.InstructionStartTime.Value).TotalSeconds;
                var totalWait = state.InitialWaitSeconds ?? 5.0;
                var remaining = Math.Max(0.0, totalWait - elapsed);

                state.RemainingWaitSeconds = remaining;
                state.Speed = 0;
                
                if (remaining <= 0)
                {
                    state.InstructionStartTime = null;
                    state.RemainingWaitSeconds = null;
                    state.InitialWaitSeconds = null;
                    state.ProgressPercentage = 100.0;
                    state.CurrentInstructionIndex++;
                    ExecuteStep(state); // Move to next immediately
                }
                else
                {
                    state.ProgressPercentage = Math.Min(100.0, (elapsed / totalWait) * 100.0);
                    state.InstructionStatus = $"Waiting ({remaining:F0}s remaining)";
                }
                break;

            case InstructionType.MoveTo:
                if (!inst.Latitude.HasValue || !inst.Longitude.HasValue)
                {
                    state.CurrentInstructionIndex++;
                    ExecuteStep(state);
                    return;
                }

                double targetLat = inst.Latitude.Value;
                double targetLng = inst.Longitude.Value;
                double speedKmh = inst.Speed ?? 50.0;
                state.Speed = speedKmh;

                // Simple flat scale approximation adjusted by cosine of mean latitude for longitudinal degrees
                double meanLatRad = (state.Latitude + targetLat) / 2.0 * (Math.PI / 180.0);
                double cosLat = Math.Cos(meanLatRad);

                double dLat = targetLat - state.Latitude;
                double dLng = targetLng - state.Longitude;
                
                // Convert coordinate differences to approximate kilometers
                // 1 deg Lat = ~111.1 km, 1 deg Lng = ~111.1 km * cos(lat)
                double distLatKm = dLat * 111.1;
                double distLngKm = dLng * 111.1 * cosLat;
                double totalDistanceKm = Math.Sqrt(distLatKm * distLatKm + distLngKm * distLngKm);

                if (!state.MoveTotalDistance.HasValue || state.MoveTotalDistance.Value == 0)
                {
                    state.MoveTotalDistance = totalDistanceKm;
                }

                // Movement during this update interval
                double timeHours = (state.UpdateIntervalMs / 1000.0) / 3600.0;
                double stepDistanceKm = speedKmh * timeHours;

                if (totalDistanceKm <= stepDistanceKm)
                {
                    // Arrived
                    state.Latitude = targetLat;
                    state.Longitude = targetLng;
                    state.Speed = 0;
                    state.MoveTotalDistance = null;
                    state.ProgressPercentage = 100.0;
                    state.CurrentInstructionIndex++;
                    state.InstructionStatus = $"Arrived at destination ({targetLat:F4}, {targetLng:F4})";
                    AddLog(state.Imei, state.InstructionStatus);
                }
                else
                {
                    // Move towards target by interpolating ratios along lat/long degrees
                    double ratio = stepDistanceKm / totalDistanceKm;
                    state.Latitude += dLat * ratio;
                    state.Longitude += dLng * ratio;
                    
                    double startDist = state.MoveTotalDistance ?? totalDistanceKm;
                    double covered = startDist - totalDistanceKm;
                    state.ProgressPercentage = Math.Clamp((covered / startDist) * 100.0, 0.0, 99.9);
                    state.InstructionStatus = $"Driving to ({targetLat:F4}, {targetLng:F4})";
                }
                break;
        }
    }

    private AvlDataPacket GeneratePacket(TrackerState state)
    {
        var extVoltageBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(extVoltageBytes, (uint)(state.Ignition ? 13800 : 12200));

        return new AvlDataPacket
        {
            CodecId = AvlCodec.Codec8Extended,
            RecordCount = 1,
            Records = new List<AvlRecord>
            {
                new()
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Priority = 1,
                    Gps = new GpsElement
                    {
                        Latitude = state.Latitude,
                        Longitude = state.Longitude,
                        Altitude = 120,
                        Angle = 90,
                        Satellites = 12,
                        Speed = (short)state.Speed
                    },
                    Io = new RawIoElement
                    {
                        EventId = 0,
                        TotalIoCount = 2,
                        Properties = new Dictionary<int, byte[]>
                        {
                            { 239, new byte[] { (byte)(state.Ignition ? 1 : 0) } },
                            { 66, extVoltageBytes }
                        }
                    }
                }
            }
        };
    }
}
