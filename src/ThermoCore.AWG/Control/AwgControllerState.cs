using ThermoCore.Core.Diagnostics;

namespace ThermoCore.AWG.Control;

/// <summary>Mutable controller memory carried between simulation steps.</summary>
public sealed record AwgControllerState
{
    public required AwgOperatingMode CurrentMode { get; init; }

    public required TimeSpan TimeInCurrentMode { get; init; }

    public required DateTimeOffset? LastModeChangeUtc { get; init; }

    public required int ConsecutiveFaultCount { get; init; }

    public required bool IsLatchedFault { get; init; }

    public required string LastTransitionReasonCode { get; init; }

    public AwgFaultCode ActiveFaultCode { get; init; } = AwgFaultCode.None;

    /// <summary>Previous TEC power request for slew-rate limiting (COOL-004).</summary>
    public double LastPeltierPowerRequestW { get; init; }

    public static AwgControllerState CreateInitial(AwgOperatingMode mode = AwgOperatingMode.Off)
        => new()
        {
            CurrentMode = mode,
            TimeInCurrentMode = TimeSpan.Zero,
            LastModeChangeUtc = null,
            ConsecutiveFaultCount = 0,
            IsLatchedFault = false,
            LastTransitionReasonCode = "INIT",
            ActiveFaultCode = AwgFaultCode.None,
            LastPeltierPowerRequestW = 0.0
        };
}
