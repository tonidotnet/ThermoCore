using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>Pure evaluation of a commercial Peltier dehumidifier black-box map.</summary>
public sealed class CommercialPeltierDehumidifierModel
{
    private readonly CommercialPeltierDehumidifierProfile _profile;
    private readonly IPsychrometricCalculator _calculator;

    public CommercialPeltierDehumidifierModel(
        CommercialPeltierDehumidifierProfile profile,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profile = profile.Validate();
        _calculator = calculator ?? new PsychrometricCalculator();
    }

    public CommercialPeltierDehumidifierProfile Profile => _profile;

    public CommercialPeltierDehumidifierModelResult Evaluate(
        MoistAirState inlet,
        double? electricalPowerOverrideW = null)
    {
        ArgumentNullException.ThrowIfNull(inlet);
        FiniteNumber.RequirePositive(inlet.DryAirMassFlowKgPerSecond, nameof(inlet.DryAirMassFlowKgPerSecond));

        var diagnostics = new List<SimulationDiagnostic>();
        var powerW = electricalPowerOverrideW ?? EstimatePowerFromInlet(inlet);
        FiniteNumber.RequireNonNegative(powerW, nameof(electricalPowerOverrideW));

        var lookup = CommercialPeltierMapInterpolator.Evaluate(
            _profile,
            new CommercialPeltierMapInterpolator.Query(
                inlet.TemperatureK,
                inlet.RelativeHumidityFraction,
                powerW,
                inlet.DryAirMassFlowKgPerSecond));

        if (lookup.OutsideValidity)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "COMMERCIAL_PELTIER.OUTSIDE_VALIDITY",
                Severity = DiagnosticSeverity.Warning,
                Message =
                    "Query was clamped to the measured validity range; no undocumented extrapolation was performed.",
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["inletTemperatureK"] = lookup.ClampedQuery.InletTemperatureK,
                    ["inletRh"] = lookup.ClampedQuery.InletRelativeHumidityFraction,
                    ["electricalPowerW"] = lookup.ClampedQuery.ElectricalPowerW
                }
            });
        }

        var map = lookup.Interpolated;
        var waterRate = Math.Max(0.0, map.WaterProductionRateKgPerSecond);
        var electrical = Math.Max(0.0, electricalPowerOverrideW ?? map.ElectricalPowerW);

        MoistAirState outlet;
        var outletSupported = _profile.SupportsOutletState
            && map.OutletTemperatureK is { } tout
            && map.OutletRelativeHumidityFraction is { } rhOut;
        if (outletSupported)
        {
            outlet = _calculator.CreateFromRelativeHumidity(
                map.OutletTemperatureK!.Value,
                inlet.PressurePa,
                map.OutletRelativeHumidityFraction!.Value,
                inlet.DryAirMassFlowKgPerSecond);

            var humidityDerivedWater = Math.Max(
                0.0,
                inlet.WaterVaporMassFlowKgPerSecond - outlet.WaterVaporMassFlowKgPerSecond);
            if (Math.Abs(humidityDerivedWater - waterRate) > Math.Max(1e-6, 0.25 * Math.Max(waterRate, humidityDerivedWater)))
            {
                diagnostics.Add(new SimulationDiagnostic
                {
                    Code = "COMMERCIAL_PELTIER.WATER_HUMIDITY_MISMATCH",
                    Severity = DiagnosticSeverity.Information,
                    Message =
                        "Map water rate differs from inlet−outlet humidity closure; liquid output uses the map rate.",
                    Values = new Dictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["mapWaterKgPerSecond"] = waterRate,
                        ["humidityDerivedWaterKgPerSecond"] = humidityDerivedWater
                    }
                });
            }
        }
        else
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "COMMERCIAL_PELTIER.OUTLET_STATE_UNSUPPORTED",
                Severity = DiagnosticSeverity.Warning,
                Message =
                    "Profile has no outlet T/RH support; outlet humidity is reduced from map water rate at inlet temperature."
            });

            var outletHumidityRatio = Math.Max(
                0.0,
                inlet.HumidityRatioKgPerKgDryAir
                    - waterRate / Math.Max(inlet.DryAirMassFlowKgPerSecond, 1e-12));
            outlet = _calculator.CreateFromHumidityRatio(
                inlet.TemperatureK,
                inlet.PressurePa,
                outletHumidityRatio,
                inlet.DryAirMassFlowKgPerSecond);
        }

        var coolingW = Math.Max(
            0.0,
            inlet.DryAirMassFlowKgPerSecond
                * (inlet.SpecificEnthalpyJPerKgDryAir - outlet.SpecificEnthalpyJPerKgDryAir));

        return new CommercialPeltierDehumidifierModelResult
        {
            Outlet = outlet,
            WaterProductionRateKgPerSecond = waterRate,
            ElectricalPowerW = electrical,
            DeliveredCoolingPowerW = coolingW,
            OutsideValidity = lookup.OutsideValidity,
            OutletStateFromMap = outletSupported,
            ColdSurfaceTemperatureK = map.ColdSurfaceTemperatureK,
            HotSideTemperatureK = map.HotSideTemperatureK,
            Diagnostics = diagnostics
        };
    }

    private double EstimatePowerFromInlet(MoistAirState inlet)
    {
        // Nearest map power at similar inlet climate (no electrical port provided).
        var nearest = _profile.MapPoints
            .OrderBy(p =>
                Math.Abs(p.InletTemperatureK - inlet.TemperatureK)
                + 10.0 * Math.Abs(p.InletRelativeHumidityFraction - inlet.RelativeHumidityFraction))
            .First();
        return nearest.ElectricalPowerW;
    }
}

/// <summary>Black-box evaluate result for one operating condition.</summary>
public sealed record CommercialPeltierDehumidifierModelResult
{
    public required MoistAirState Outlet { get; init; }

    public required double WaterProductionRateKgPerSecond { get; init; }

    public required double ElectricalPowerW { get; init; }

    public required double DeliveredCoolingPowerW { get; init; }

    public required bool OutsideValidity { get; init; }

    public required bool OutletStateFromMap { get; init; }

    public double? ColdSurfaceTemperatureK { get; init; }

    public double? HotSideTemperatureK { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}
