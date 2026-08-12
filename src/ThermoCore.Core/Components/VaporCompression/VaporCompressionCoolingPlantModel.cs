using ThermoCore.Core.Balances;
using ThermoCore.Core.Components;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>
/// Map-based vapor-compression cooling plant: performance map + condenser psychrometrics + cycling
/// (COOL-006 / COOL-007 / R5-002). Not a refrigerant property solver.
/// </summary>
public sealed class VaporCompressionCoolingPlantModel
{
    private readonly VaporCompressionPerformanceMap _map;
    private readonly VaporCompressionMapEvaluator _mapEvaluator;
    private readonly VaporCompressionCyclingController _cycling;
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _bypassFactor;
    private readonly double _drainageEfficiency;
    private readonly double _maximumRetainedFilmKg;
    private readonly CondenserComponent _evaporatorCoil;

    public VaporCompressionCoolingPlantModel(
        VaporCompressionPerformanceMap map,
        double bypassFactor = 0.1,
        double drainageEfficiency = 0.95,
        double maximumRetainedFilmKg = 0.05,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        FiniteNumber.Require(bypassFactor, nameof(bypassFactor));
        if (bypassFactor is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(bypassFactor));
        }

        FiniteNumber.Require(drainageEfficiency, nameof(drainageEfficiency));
        if (drainageEfficiency is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(drainageEfficiency));
        }

        FiniteNumber.RequireNonNegative(maximumRetainedFilmKg, nameof(maximumRetainedFilmKg));

        _map = map.Validate();
        _mapEvaluator = new VaporCompressionMapEvaluator(_map);
        _cycling = new VaporCompressionCyclingController(_map.Cycling);
        _calculator = calculator ?? new PsychrometricCalculator();
        _bypassFactor = bypassFactor;
        _drainageEfficiency = drainageEfficiency;
        _maximumRetainedFilmKg = maximumRetainedFilmKg;

        var fallbackSurface = 0.5 * (_map.Validity.MinimumEvaporatingTemperatureK
            + _map.Validity.MaximumEvaporatingTemperatureK);
        _evaporatorCoil = new CondenserComponent(
            id: "vc-evaporator-coil",
            bypassFactor: _bypassFactor,
            drainageEfficiency: _drainageEfficiency,
            fallbackSurfaceTemperatureK: fallbackSurface,
            fallbackAvailableCoolingPowerW: 0.0,
            maximumRetainedFilmKg: _maximumRetainedFilmKg,
            filmCarryoverFraction: 0.0,
            calculator: _calculator);
        Reset();
    }

    public VaporCompressionPerformanceMap Map => _map;

    public bool CompressorIsOn => _cycling.IsOn;

    public void Reset()
    {
        _cycling.Reset();
        _evaporatorCoil.Initialize(new SimulationContext
        {
            SimulationStart = DateTimeOffset.UnixEpoch,
            TimeStep = TimeSpan.FromSeconds(1),
            ElapsedTime = TimeSpan.Zero,
            StepIndex = 0
        });
    }

    public VaporCompressionPlantStepResult Evaluate(VaporCompressionPlantStepRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Inlet);
        ArgumentNullException.ThrowIfNull(request.Simulation);
        FiniteNumber.RequirePositive(request.EvaporatingTemperatureK, nameof(request.EvaporatingTemperatureK));
        FiniteNumber.RequirePositive(request.CondensingTemperatureK, nameof(request.CondensingTemperatureK));
        FiniteNumber.Require(request.SpeedFraction, nameof(request.SpeedFraction));
        if (request.SpeedFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.SpeedFraction));
        }

        var diagnostics = new List<SimulationDiagnostic>();
        var requestedOn = request.CompressorRequested && request.SpeedFraction > 1e-9;
        var cycle = _cycling.Step(requestedOn, request.Simulation.TimeStep);
        diagnostics.AddRange(cycle.Diagnostics);

        var mapResult = _mapEvaluator.Evaluate(
            request.EvaporatingTemperatureK,
            request.CondensingTemperatureK,
            request.SpeedFraction,
            request.DischargeTemperatureK);
        diagnostics.AddRange(mapResult.Diagnostics);

        var compressorOn = cycle.CompressorOn && !mapResult.Rejected;
        var mapQc = compressorOn ? mapResult.CoolingCapacityW : 0.0;
        var mapPe = compressorOn ? mapResult.ElectricalPowerW : 0.0;
        var moduleFanW = compressorOn ? Math.Max(0.0, _map.FanElectricalPowerW ?? 0.0) : 0.0;
        var electricalW = mapPe + moduleFanW;
        var processFanW = Math.Max(0.0, request.ProcessFanElectricalPowerW);

        if (cycle.CompressorOn && mapResult.Rejected)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "VC.COMPRESSOR_FORCED_OFF",
                Severity = DiagnosticSeverity.Warning,
                Message = "Compressor gated off because the map rejected the operating point."
            });
        }

        // Retain film/state across steps; only Reset() re-initializes the coil.
        var step = _evaporatorCoil.Evaluate(new ComponentStepContext
        {
            Simulation = request.Simulation,
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["inlet"] = request.Inlet,
                ["cooling"] = new HeatFlowState
                {
                    HeatFlowW = mapQc,
                    TemperatureK = request.EvaporatingTemperatureK
                }
            }
        });
        _evaporatorCoil.Commit(step);
        diagnostics.AddRange(step.Diagnostics);

        if (step.OutputStates.TryGetValue("outlet", out var outletRaw) is not true
            || outletRaw is not MoistAirState outlet)
        {
            return new VaporCompressionPlantStepResult
            {
                Outlet = request.Inlet,
                CollectedWaterKgPerSecond = 0.0,
                CoolingDeliveredW = 0.0,
                DeviceCoolingCapacityW = mapQc,
                ElectricalInputW = electricalW,
                RejectedHeatW = mapQc + electricalW,
                MapCop = compressorOn ? mapResult.Cop : null,
                CompressorOn = compressorOn,
                HeldByCyclingLimits = cycle.HeldByCyclingLimits,
                OutsideValidity = mapResult.OutsideValidity,
                Rejected = mapResult.Rejected,
                Balance = step.Balance,
                Diagnostics = diagnostics,
                LiquidOut = null,
                MapEvaluation = mapResult
            };
        }

        step.OutputStates.TryGetValue("liquid_out", out var liquidRaw);
        var liquid = liquidRaw as LiquidWaterState;
        var waterRate = liquid?.MassFlowKgPerSecond
            ?? Math.Max(0.0, _evaporatorCoil.LastCollectedWaterRateKgPerSecond);

        var deliveredW = Math.Max(
            0.0,
            request.Inlet.DryAirMassFlowKgPerSecond
                * (request.Inlet.SpecificEnthalpyJPerKgDryAir - outlet.SpecificEnthalpyJPerKgDryAir));
        var deviceQcW = Math.Max(0.0, _evaporatorCoil.LastTotalCoolingPowerW);
        var rejectedW = deviceQcW + electricalW;
        var dt = request.Simulation.TimeStep.TotalSeconds;

        // Keep condenser mass bookkeeping; add electrical energy so residuals close.
        var balance = ConservationBalance.FromTerms(
            step.Balance.DryAirMassInputKg,
            step.Balance.DryAirMassOutputKg,
            step.Balance.DryAirMassStorageChangeKg,
            step.Balance.WaterMassInputKg,
            step.Balance.WaterMassOutputKg,
            step.Balance.WaterMassStorageChangeKg,
            energyInputJ: step.Balance.EnergyInputJ + electricalW * dt,
            energyOutputJ: step.Balance.EnergyOutputJ - deviceQcW * dt + rejectedW * dt,
            storedEnergyChangeJ: step.Balance.StoredEnergyChangeJ,
            electricalEnergyInputJ: electricalW * dt,
            electricalEnergyOutputJ: electricalW * dt);

        return new VaporCompressionPlantStepResult
        {
            Outlet = outlet,
            CollectedWaterKgPerSecond = waterRate,
            CoolingDeliveredW = deliveredW,
            DeviceCoolingCapacityW = deviceQcW,
            ElectricalInputW = electricalW,
            RejectedHeatW = rejectedW,
            MapCop = compressorOn ? mapResult.Cop : null,
            BareDeviceCop = electricalW > 0.0 ? deviceQcW / electricalW : null,
            PlantCop = (electricalW + processFanW) > 0.0
                ? deliveredW / (electricalW + processFanW)
                : null,
            CompressorOn = compressorOn,
            HeldByCyclingLimits = cycle.HeldByCyclingLimits,
            OutsideValidity = mapResult.OutsideValidity,
            Rejected = mapResult.Rejected,
            Balance = balance,
            Diagnostics = diagnostics,
            LiquidOut = liquid,
            MapEvaluation = mapResult,
            ProcessFanElectricalPowerW = processFanW
        };
    }
}

/// <summary>One plant-step request for the vapor-compression cooling plant.</summary>
public sealed record VaporCompressionPlantStepRequest
{
    public required MoistAirState Inlet { get; init; }

    public required SimulationContext Simulation { get; init; }

    public required double EvaporatingTemperatureK { get; init; }

    public required double CondensingTemperatureK { get; init; }

    public double SpeedFraction { get; init; } = 1.0;

    public bool CompressorRequested { get; init; } = true;

    public double? DischargeTemperatureK { get; init; }

    public double ProcessFanElectricalPowerW { get; init; }
}

/// <summary>One plant-step result for the vapor-compression cooling plant.</summary>
public sealed record VaporCompressionPlantStepResult
{
    public required MoistAirState Outlet { get; init; }

    public required double CollectedWaterKgPerSecond { get; init; }

    public required double CoolingDeliveredW { get; init; }

    public required double DeviceCoolingCapacityW { get; init; }

    public required double ElectricalInputW { get; init; }

    public required double RejectedHeatW { get; init; }

    public double? MapCop { get; init; }

    public double? BareDeviceCop { get; init; }

    public double? PlantCop { get; init; }

    public required bool CompressorOn { get; init; }

    public required bool HeldByCyclingLimits { get; init; }

    public required bool OutsideValidity { get; init; }

    public required bool Rejected { get; init; }

    public required ConservationBalance Balance { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }

    public LiquidWaterState? LiquidOut { get; init; }

    public required VaporCompressionMapEvaluationResult MapEvaluation { get; init; }

    public double ProcessFanElectricalPowerW { get; init; }
}
