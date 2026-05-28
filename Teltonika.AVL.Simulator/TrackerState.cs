using System;
using System.Collections.Generic;

namespace Teltonika.AVL.Simulator;

public class TrackerState
{
    public string Imei { get; set; } = null!;
    public bool IsRunning { get; set; }
    public double Latitude { get; set; } = 56.2084;
    public double Longitude { get; set; } = 10.0359;
    public double Speed { get; set; } // km/h
    public bool Ignition { get; set; }
    public int UpdateIntervalMs { get; set; } = 2000;
    
    public List<TrackerInstruction> Instructions { get; set; } = new();
    public int CurrentInstructionIndex { get; set; }
    public string InstructionStatus { get; set; } = "Idle";
    
    // Internal trackers for simulator state machine
    public DateTime? InstructionStartTime { get; set; }
    public double? RemainingWaitSeconds { get; set; }
    public double? InitialWaitSeconds { get; set; }
    public double? MoveTotalDistance { get; set; }
    public double ProgressPercentage { get; set; }
}
