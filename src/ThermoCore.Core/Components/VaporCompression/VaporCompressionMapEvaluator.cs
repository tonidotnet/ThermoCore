using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>Pure map evaluation with frost/safety diagnostics (R5-001 contract).</summary>
public sealed class VaporCompressionMapEvaluator
{
    private readonly VaporCompressionPerformanceMap _map;

    public VaporCompressionMapEvaluator(VaporCompressionPerformanceMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        _map = map.Validate();
    }

    public VaporCompressionPerformanceMap Map => _map;

    public VaporCompressionMapEvaluationResult Evaluate(
        double evaporatingTemperatureK,
        double condensingTemperatureK,
        double speedFraction,
        double? dischargeTemperatureK = null)
    {
        FiniteNumber.RequirePositive(evaporatingTemperatureK, nameof(evaporatingTemperatureK));
        FiniteNumber.RequirePositive(condensingTemperatureK, nameof(condensingTemperatureK));
        FiniteNumber.Require(speedFraction, nameof(speedFraction));

        var diagnostics = new List<SimulationDiagnostic>();
        var lookup = VaporCompressionMapInterpolator.Evaluate(
            _map,
            new VaporCompressionMapInterpolator.Query(
                evaporatingTemperatureK,
                condensingTemperatureK,
                speedFraction));

        if (lookup.Rejected)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "VC.EXTRAPOLATION_REJECTED",
                Severity = DiagnosticSeverity.Error,
                Message =
                    "Query is outside the measured vapor-compression map validity range; extrapolation is rejected by policy.",
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["evaporatingTemperatureK"] = evaporatingTemperatureK,
                    ["condensingTemperatureK"] = condensingTemperatureK,
                    ["speedFraction"] = speedFraction
                }
            });
        }
        else if (lookup.OutsideValidity)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "VC.OUTSIDE_VALIDITY",
                Severity = DiagnosticSeverity.Warning,
                Message =
                    "Query was clamped to the measured validity range; no undocumented extrapolation was performed.",
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["evaporatingTemperatureK"] = lookup.EffectiveQuery.EvaporatingTemperatureK,
                    ["condensingTemperatureK"] = lookup.EffectiveQuery.CondensingTemperatureK,
                    ["speedFraction"] = lookup.EffectiveQuery.SpeedFraction
                }
            });
        }

        var safety = _map.Safety;
        if (safety.FrostThresholdEvaporatingTemperatureK is { } frost
            && evaporatingTemperatureK <= frost)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "VC.FROST_RISK",
                Severity = DiagnosticSeverity.Warning,
                Message = "Evaporating temperature is at or below the frost threshold.",
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["evaporatingTemperatureK"] = evaporatingTemperatureK,
                    ["frostThresholdK"] = frost
                }
            });
        }

        if (safety.MaximumCondensingTemperatureK is { } tmax
            && condensingTemperatureK > tmax)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "VC.CONDENSING_TEMPERATURE_HIGH",
                Severity = DiagnosticSeverity.Warning,
                Message = "Condensing temperature exceeds the configured safety maximum.",
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["condensingTemperatureK"] = condensingTemperatureK,
                    ["maximumCondensingTemperatureK"] = tmax
                }
            });
        }

        if (dischargeTemperatureK is { } td)
        {
            FiniteNumber.RequirePositive(td, nameof(dischargeTemperatureK));
            if (safety.MaximumDischargeTemperatureK is { } tdMax && td > tdMax)
            {
                diagnostics.Add(new SimulationDiagnostic
                {
                    Code = "VC.DISCHARGE_TEMPERATURE_HIGH",
                    Severity = DiagnosticSeverity.Warning,
                    Message = "Discharge temperature exceeds the configured safety maximum.",
                    Values = new Dictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["dischargeTemperatureK"] = td,
                        ["maximumDischargeTemperatureK"] = tdMax
                    }
                });
            }
        }

        var point = lookup.Interpolated;
        return new VaporCompressionMapEvaluationResult
        {
            CoolingCapacityW = point.CoolingCapacityW,
            ElectricalPowerW = point.ElectricalPowerW,
            Cop = lookup.Rejected ? null : point.EffectiveCop,
            OutsideValidity = lookup.OutsideValidity,
            Rejected = lookup.Rejected,
            EffectiveQuery = lookup.EffectiveQuery,
            InterpolatedPoint = point,
            Diagnostics = diagnostics,
            MinimumRuntime = _map.Cycling.MinimumRuntime,
            MinimumOffTime = _map.Cycling.MinimumOffTime
        };
    }
}

/// <summary>Result of a vapor-compression map lookup.</summary>
public sealed record VaporCompressionMapEvaluationResult
{
    public required double CoolingCapacityW { get; init; }

    public required double ElectricalPowerW { get; init; }

    public double? Cop { get; init; }

    public required bool OutsideValidity { get; init; }

    public required bool Rejected { get; init; }

    public required VaporCompressionMapInterpolator.Query EffectiveQuery { get; init; }

    public required VaporCompressionMapPoint InterpolatedPoint { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }

    public required TimeSpan MinimumRuntime { get; init; }

    public required TimeSpan MinimumOffTime { get; init; }
}
