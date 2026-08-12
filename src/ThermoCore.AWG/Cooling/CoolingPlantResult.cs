using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.AWG.Cooling;

/// <summary>Common cooling-plant orchestration result (ADR-016 / R4-001).</summary>
public sealed record CoolingPlantResult
{
    public required CoolingTechnology Technology { get; init; }

    public required MoistAirState Outlet { get; init; }

    public required double CollectedWaterKgPerSecond { get; init; }

    /// <summary>Air-side cooling delivered = m_da · (h_in − h_out) (W).</summary>
    public required double CoolingDeliveredW { get; init; }

    public required double ElectricalInputW { get; init; }

    /// <summary>Plant thermal input; equals delivered air-side cooling for current adapters.</summary>
    public required double ThermalInputW { get; init; }

    public required double RejectedHeatW { get; init; }

    /// <summary>Bare device COP = device Qc / Pe when both &gt; 0.</summary>
    public double? BareDeviceCop { get; init; }

    /// <summary>Plant COP = thermal / (Pe + fan) when both &gt; 0.</summary>
    public double? CoolingPlantCop { get; init; }

    public double PressureDropPa { get; init; }

    public required ConservationBalance Balance { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }

    public LiquidWaterState? LiquidOut { get; init; }

    public double? DeviceCoolingCapacityW { get; init; }

    public IReadOnlyDictionary<string, double> TechnologySpecificValues { get; init; }
        = new Dictionary<string, double>(StringComparer.Ordinal);
}
