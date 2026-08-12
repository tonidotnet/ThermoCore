using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Balances;
using ThermoCore.Core.Components;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Cooling;

/// <summary>
/// Thermoelectric adapter over the existing Condenser + ControllableHeatSource proxy
/// (does not move Peltier α/R/K equations — COOL-001 / R4-001).
/// </summary>
public sealed class ThermoelectricCoolingPlantAdapter : ICoolingPlantModel
{
    private readonly AwgCondenserParameters _condenser;
    private readonly IPsychrometricCalculator _calculator;
    private readonly CondenserComponent _component;

    public ThermoelectricCoolingPlantAdapter(
        AwgCondenserParameters condenserParameters,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentNullException.ThrowIfNull(condenserParameters);
        _condenser = condenserParameters.Validate();
        _calculator = calculator ?? new PsychrometricCalculator();
        _component = new CondenserComponent(
            id: AwgV3TopologyIds.Condenser,
            bypassFactor: _condenser.BypassFactor,
            drainageEfficiency: _condenser.DrainageEfficiency,
            fallbackSurfaceTemperatureK: _condenser.FallbackSurfaceTemperatureK,
            fallbackAvailableCoolingPowerW: _condenser.FallbackAvailableCoolingPowerW,
            maximumRetainedFilmKg: _condenser.MaximumRetainedFilmKg,
            filmCarryoverFraction: _condenser.FilmCarryoverFraction,
            calculator: _calculator);
    }

    public CoolingTechnology Technology => CoolingTechnology.Thermoelectric;

    public CoolingPlantResult Evaluate(CoolingPlantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Inlet);
        ArgumentNullException.ThrowIfNull(request.Simulation);

        var coolingCapacityW = request.AvailableCoolingPowerW
            ?? request.ElectricalPowerW
            ?? _condenser.FallbackAvailableCoolingPowerW;
        FiniteNumber.RequireNonNegative(coolingCapacityW, nameof(request.AvailableCoolingPowerW));

        var surfaceK = request.ColdSurfaceTemperatureK ?? _condenser.FallbackSurfaceTemperatureK;
        FiniteNumber.RequirePositive(surfaceK, nameof(request.ColdSurfaceTemperatureK));

        // Proxy TEC accounting used by AWG V3 today: Pe ≈ Qc.
        var electricalW = request.ElectricalPowerW ?? coolingCapacityW;
        FiniteNumber.RequireNonNegative(electricalW, nameof(request.ElectricalPowerW));

        _component.Initialize(request.Simulation);
        var step = _component.Evaluate(new ComponentStepContext
        {
            Simulation = request.Simulation,
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["inlet"] = request.Inlet,
                ["cooling"] = new HeatFlowState
                {
                    HeatFlowW = coolingCapacityW,
                    TemperatureK = surfaceK
                }
            }
        });
        _component.Commit(step);

        if (step.OutputStates.TryGetValue("outlet", out var outletRaw) is not true
            || outletRaw is not MoistAirState outlet)
        {
            return Failed(request, electricalW, coolingCapacityW, step.Diagnostics, step.Balance);
        }

        step.OutputStates.TryGetValue("liquid_out", out var liquidRaw);
        var liquid = liquidRaw as LiquidWaterState;
        var waterRate = liquid?.MassFlowKgPerSecond
            ?? Math.Max(0.0, _component.LastCollectedWaterRateKgPerSecond);

        var deliveredW = Math.Max(
            0.0,
            request.Inlet.DryAirMassFlowKgPerSecond
                * (request.Inlet.SpecificEnthalpyJPerKgDryAir - outlet.SpecificEnthalpyJPerKgDryAir));
        var deviceQcW = Math.Max(0.0, _component.LastTotalCoolingPowerW);
        var rejectedW = deviceQcW + electricalW;
        var fanW = Math.Max(0.0, request.FanElectricalPowerW ?? 0.0);

        return new CoolingPlantResult
        {
            Technology = Technology,
            Outlet = outlet,
            CollectedWaterKgPerSecond = waterRate,
            CoolingDeliveredW = deliveredW,
            ElectricalInputW = electricalW,
            ThermalInputW = deliveredW,
            RejectedHeatW = rejectedW,
            BareDeviceCop = AwgCoolingMetricsCalculator.BareCoolingDeviceCop(deviceQcW, electricalW),
            CoolingPlantCop = AwgPerformanceKpiCalculator.RatioOrNull(deliveredW, electricalW + fanW),
            PressureDropPa = 0.0,
            Balance = step.Balance,
            Diagnostics = step.Diagnostics,
            LiquidOut = liquid,
            DeviceCoolingCapacityW = deviceQcW,
            TechnologySpecificValues = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["availableCoolingPowerW"] = coolingCapacityW,
                ["coldSurfaceTemperatureK"] = surfaceK,
                ["proxyElectricalEqualsCooling"] = electricalW == coolingCapacityW ? 1.0 : 0.0
            }
        };
    }

    private CoolingPlantResult Failed(
        CoolingPlantRequest request,
        double electricalW,
        double coolingCapacityW,
        IReadOnlyList<SimulationDiagnostic> diagnostics,
        ConservationBalance balance)
        => new()
        {
            Technology = Technology,
            Outlet = request.Inlet,
            CollectedWaterKgPerSecond = 0.0,
            CoolingDeliveredW = 0.0,
            ElectricalInputW = electricalW,
            ThermalInputW = 0.0,
            RejectedHeatW = coolingCapacityW + electricalW,
            Balance = balance,
            Diagnostics = diagnostics,
            DeviceCoolingCapacityW = 0.0
        };
}
