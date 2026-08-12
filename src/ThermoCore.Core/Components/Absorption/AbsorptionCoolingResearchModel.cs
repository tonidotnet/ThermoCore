using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Absorption;

/// <summary>
/// Research-only absorption feasibility evaluator (R7-001 / COOL-008).
/// Emits <c>ABSORPTION.RESEARCH_ONLY</c>; does not provide a production AWG plant path.
/// </summary>
public sealed class AbsorptionCoolingResearchModel
{
    private readonly AbsorptionPerformanceMap _map;

    public AbsorptionCoolingResearchModel(AbsorptionPerformanceMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        _map = map.Validate();
    }

    public AbsorptionPerformanceMap Map => _map;

    public AbsorptionFeasibilityResult Evaluate(
        double generatorTemperatureK,
        double heatSinkTemperatureK,
        double evaporatorTemperatureK)
    {
        FiniteNumber.RequirePositive(generatorTemperatureK, nameof(generatorTemperatureK));
        FiniteNumber.RequirePositive(heatSinkTemperatureK, nameof(heatSinkTemperatureK));
        FiniteNumber.RequirePositive(evaporatorTemperatureK, nameof(evaporatorTemperatureK));

        var diagnostics = new List<SimulationDiagnostic>
        {
            new()
            {
                Code = "ABSORPTION.RESEARCH_ONLY",
                Severity = DiagnosticSeverity.Information,
                Message =
                    "Absorption cooling remains research-only (COOL-008). This map is a feasibility screen, not a production plant model."
            }
        };

        var lookup = AbsorptionMapInterpolator.Evaluate(
            _map,
            new AbsorptionMapInterpolator.Query(
                generatorTemperatureK,
                heatSinkTemperatureK,
                evaporatorTemperatureK));

        if (lookup.Rejected)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "ABSORPTION.EXTRAPOLATION_REJECTED",
                Severity = DiagnosticSeverity.Error,
                Message = "Query is outside the absorption research map; extrapolation is rejected.",
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["generatorTemperatureK"] = generatorTemperatureK,
                    ["heatSinkTemperatureK"] = heatSinkTemperatureK,
                    ["evaporatorTemperatureK"] = evaporatorTemperatureK
                }
            });
        }
        else if (lookup.OutsideValidity)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "ABSORPTION.OUTSIDE_VALIDITY",
                Severity = DiagnosticSeverity.Warning,
                Message = "Query was clamped to the measured validity range; no undocumented extrapolation was performed.",
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["generatorTemperatureK"] = lookup.EffectiveQuery.GeneratorTemperatureK,
                    ["heatSinkTemperatureK"] = lookup.EffectiveQuery.HeatSinkTemperatureK,
                    ["evaporatorTemperatureK"] = lookup.EffectiveQuery.EvaporatorTemperatureK
                }
            });
        }

        var point = lookup.Interpolated;
        var feasible = !lookup.Rejected
            && point.CoolingOutputW > 0.0
            && point.ThermalInputW > 0.0
            && point.EffectiveThermalCop > 0.0;

        if (!feasible && !lookup.Rejected)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "ABSORPTION.NOT_FEASIBLE",
                Severity = DiagnosticSeverity.Warning,
                Message = "Map point yields non-positive cooling or thermal COP."
            });
        }

        return new AbsorptionFeasibilityResult
        {
            ResearchOnly = true,
            Feasible = feasible,
            ThermalInputW = lookup.Rejected ? 0.0 : point.ThermalInputW,
            CoolingOutputW = lookup.Rejected ? 0.0 : point.CoolingOutputW,
            ThermalCop = lookup.Rejected ? null : point.EffectiveThermalCop,
            OutsideValidity = lookup.OutsideValidity,
            Rejected = lookup.Rejected,
            EffectiveQuery = lookup.EffectiveQuery,
            InterpolatedPoint = point,
            Diagnostics = diagnostics
        };
    }
}

/// <summary>Feasibility screen result for absorption research maps.</summary>
public sealed record AbsorptionFeasibilityResult
{
    public required bool ResearchOnly { get; init; }

    public required bool Feasible { get; init; }

    public required double ThermalInputW { get; init; }

    public required double CoolingOutputW { get; init; }

    public double? ThermalCop { get; init; }

    public required bool OutsideValidity { get; init; }

    public required bool Rejected { get; init; }

    public required AbsorptionMapInterpolator.Query EffectiveQuery { get; init; }

    public required AbsorptionMapPoint InterpolatedPoint { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}
