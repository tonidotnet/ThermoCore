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

    public static AwgControllerState CreateInitial(AwgOperatingMode mode = AwgOperatingMode.Off)
        => new()
        {
            CurrentMode = mode,
            TimeInCurrentMode = TimeSpan.Zero,
            LastModeChangeUtc = null,
            ConsecutiveFaultCount = 0,
            IsLatchedFault = false,
            LastTransitionReasonCode = "INIT",
            ActiveFaultCode = AwgFaultCode.None
        };
}

/// <summary>Auditable decision record for one transition or protection action.</summary>
public sealed record AwgDecisionTraceEntry
{
    public required string ReasonCode { get; init; }

    public required string PreviousMode { get; init; }

    public required string RequestedMode { get; init; }

    public required string ActiveLimitingConstraint { get; init; }

    public required IReadOnlyDictionary<string, double> ScalarInputs { get; init; }
}

/// <summary>Controller evaluation output for one timestep.</summary>
public sealed record AwgControlStepResult
{
    public required AwgControlRequest Request { get; init; }

    public required AwgControllerState ProposedState { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> Diagnostics { get; init; }

    public required IReadOnlyCollection<AwgDecisionTraceEntry> DecisionTrace { get; init; }
}

public interface IAwgController
{
    AwgControlStepResult Evaluate(
        AwgSystemObservation observation,
        AwgControllerState currentState,
        AwgControlParameters parameters,
        TimeSpan timeStep);
}
