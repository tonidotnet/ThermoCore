using ThermoCore.Core.Diagnostics;

namespace ThermoCore.AWG.Control;

/// <summary>Requested actuator setpoints for one control step.</summary>
public sealed record AwgControlRequest
{
    public required AwgOperatingMode RequestedMode { get; init; }

    public required double FanControlFraction { get; init; }

    public required double PeltierPowerRequestW { get; init; }

    public required double RecirculationFraction { get; init; }

    public required bool HeatRecoveryBypassOpen { get; init; }

    public required bool AdsorptionBedEnabled { get; init; }

    public required bool RegenerationHeatEnabled { get; init; }

    public required bool CondenserEnabled { get; init; }

    public required string ReasonCode { get; init; }

    public AwgFaultCode ActiveFaultCode { get; init; } = AwgFaultCode.None;
}
