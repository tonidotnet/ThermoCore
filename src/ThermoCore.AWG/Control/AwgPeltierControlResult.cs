using ThermoCore.Core.Diagnostics;

namespace ThermoCore.AWG.Control;

/// <summary>Resolved dew-point-tracking TEC drive for one control step (COOL-004).</summary>
public sealed record AwgPeltierControlResult
{
    public required double PowerRequestW { get; init; }

    /// <summary>T_surface,target = T_dp,in − approach margin.</summary>
    public required double TargetSurfaceTemperatureK { get; init; }

    /// <summary>Observed T_dp,in − T_surface (positive ⇒ surface colder than dew point).</summary>
    public required double DewPointMarginK { get; init; }

    public required bool PowerSaturated { get; init; }

    public required bool TargetUnreachable { get; init; }

    public required string ActiveLimitingConstraint { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}
