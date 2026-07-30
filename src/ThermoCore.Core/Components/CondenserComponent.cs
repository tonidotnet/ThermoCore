using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Moist-air condenser with bypass-factor ideal outlet, cooling-power limiting,
/// and drainage efficiency (docs/03_Components/10_Condenser.md).
/// </summary>
public sealed class CondenserComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _bypassFactor;
    private readonly double _drainageEfficiency;
    private readonly double _fallbackSurfaceTemperatureK;
    private readonly double _fallbackAvailableCoolingPowerW;
    private readonly double _maximumRetainedFilmKg;
    private readonly double _filmCarryoverFraction;
    private readonly double _heatTransferUaWPerK;
    private readonly double _massTransferEffectivenessFraction;
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private double _retainedFilmKg;

    public CondenserComponent(
        string id,
        double bypassFactor,
        double drainageEfficiency,
        double fallbackSurfaceTemperatureK,
        double fallbackAvailableCoolingPowerW,
        double maximumRetainedFilmKg = 0.05,
        double filmCarryoverFraction = 0.0,
        double heatTransferUaWPerK = 0.0,
        double massTransferEffectivenessFraction = 1.0,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(bypassFactor, nameof(bypassFactor));
        if (bypassFactor is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(bypassFactor), "Bypass factor must be in [0, 1].");
        }

        FiniteNumber.Require(drainageEfficiency, nameof(drainageEfficiency));
        if (drainageEfficiency is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(drainageEfficiency), "Drainage efficiency must be in [0, 1].");
        }

        FiniteNumber.RequirePositive(fallbackSurfaceTemperatureK, nameof(fallbackSurfaceTemperatureK));
        FiniteNumber.RequireNonNegative(fallbackAvailableCoolingPowerW, nameof(fallbackAvailableCoolingPowerW));
        FiniteNumber.RequireNonNegative(maximumRetainedFilmKg, nameof(maximumRetainedFilmKg));
        FiniteNumber.Require(filmCarryoverFraction, nameof(filmCarryoverFraction));
        if (filmCarryoverFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(filmCarryoverFraction), "Film carryover fraction must be in [0, 1].");
        }

        FiniteNumber.RequireNonNegative(heatTransferUaWPerK, nameof(heatTransferUaWPerK));
        FiniteNumber.Require(massTransferEffectivenessFraction, nameof(massTransferEffectivenessFraction));
        if (massTransferEffectivenessFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(massTransferEffectivenessFraction));
        }

        Id = id;
        _bypassFactor = bypassFactor;
        _drainageEfficiency = drainageEfficiency;
        _fallbackSurfaceTemperatureK = fallbackSurfaceTemperatureK;
        _fallbackAvailableCoolingPowerW = fallbackAvailableCoolingPowerW;
        _maximumRetainedFilmKg = maximumRetainedFilmKg;
        _filmCarryoverFraction = filmCarryoverFraction;
        _heatTransferUaWPerK = heatTransferUaWPerK;
        _massTransferEffectivenessFraction = massTransferEffectivenessFraction;
        _calculator = calculator ?? new PsychrometricCalculator();

        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir),
            new PhysicalPort("cooling", id, PortDirection.Input, PhysicalDomain.Heat, isRequired: false),
            new PhysicalPort("liquid_out", id, PortDirection.Output, PhysicalDomain.LiquidWater)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastTotalCoolingPowerW { get; private set; }

    public double LastCondensedWaterRateKgPerSecond { get; private set; }

    public double LastCollectedWaterRateKgPerSecond { get; private set; }

    public double LastUncollectedWaterRateKgPerSecond { get; private set; }

    public double LastRetainedFilmKg { get; private set; }

    public double LastEffectiveDrainageEfficiency { get; private set; }

    public double LastCarryoverWaterRateKgPerSecond { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastTotalCoolingPowerW = 0.0;
        LastCondensedWaterRateKgPerSecond = 0.0;
        LastCollectedWaterRateKgPerSecond = 0.0;
        LastUncollectedWaterRateKgPerSecond = 0.0;
        LastRetainedFilmKg = _retainedFilmKg;
        LastEffectiveDrainageEfficiency = _drainageEfficiency;
        LastCarryoverWaterRateKgPerSecond = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();

        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return Error(context, "COMPONENT.MISSING_INLET", "Condenser requires MoistAirState on 'inlet'.", "inlet");
        }

        if (inlet.DryAirMassFlowKgPerSecond <= 0.0)
        {
            return Error(context, "COMPONENT.ZERO_FLOW", "Condenser requires positive dry-air mass flow.", "inlet");
        }

        var surfaceTemperatureK = _fallbackSurfaceTemperatureK;
        var availableCoolingPowerW = _fallbackAvailableCoolingPowerW;
        if (context.InputStates.TryGetValue("cooling", out var coolingRaw)
            && coolingRaw is HeatFlowState cooling)
        {
            FiniteNumber.RequirePositive(cooling.TemperatureK, nameof(cooling.TemperatureK));
            FiniteNumber.RequireNonNegative(cooling.HeatFlowW, nameof(cooling.HeatFlowW));
            surfaceTemperatureK = cooling.TemperatureK;
            availableCoolingPowerW = cooling.HeatFlowW;
        }

        var condensationPossible = surfaceTemperatureK < inlet.DewPointTemperatureK;
        if (!condensationPossible)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "CONDENSER.NO_CONDENSATION",
                Severity = DiagnosticSeverity.Information,
                Message = "Surface temperature is at or above inlet dew point; sensible cooling only.",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex
            });
        }

        var idealOutlet = BuildIdealOutlet(inlet, surfaceTemperatureK, condensationPossible);
        var idealCoolingW = CoolingDemandW(inlet, idealOutlet);

        MoistAirState outlet;
        if (idealCoolingW <= availableCoolingPowerW + 1e-9)
        {
            outlet = idealOutlet;
        }
        else
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "CONDENSER.COOLING_POWER_LIMITED",
                Severity = DiagnosticSeverity.Warning,
                Message = "Available cooling power limits outlet approach to the ideal apparatus state.",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["idealCoolingW"] = idealCoolingW,
                    ["availableCoolingW"] = availableCoolingPowerW
                }
            });
            outlet = SolvePowerLimitedOutlet(inlet, idealOutlet, availableCoolingPowerW);
        }

        var condensed = Math.Max(0.0, inlet.WaterVaporMassFlowKgPerSecond - outlet.WaterVaporMassFlowKgPerSecond);
        var dt = context.Simulation.TimeStep.TotalSeconds;

        // Drainage efficiency rises toward 1 as retained film approaches capacity (COND-006).
        var filmFillFraction = _maximumRetainedFilmKg > 0.0
            ? Math.Clamp(_retainedFilmKg / _maximumRetainedFilmKg, 0.0, 1.0)
            : 1.0;
        var effectiveDrainageEfficiency = _drainageEfficiency
            + (1.0 - _drainageEfficiency) * filmFillFraction;
        var collected = effectiveDrainageEfficiency * condensed;
        var uncollected = condensed - collected;

        var proposedFilmKg = _retainedFilmKg + uncollected * dt;
        var carryoverRate = 0.0;
        if (_maximumRetainedFilmKg > 0.0 && proposedFilmKg > _maximumRetainedFilmKg)
        {
            var overflowKg = proposedFilmKg - _maximumRetainedFilmKg;
            proposedFilmKg = _maximumRetainedFilmKg;
            // Overflow beyond film capacity is forced into drainage collection.
            collected += overflowKg / dt;
            uncollected = Math.Max(0.0, condensed - collected);
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "CONDENSER.FILM_CAPACITY_OVERFLOW",
                Severity = DiagnosticSeverity.Information,
                Message = "Retained film reached capacity; overflow was drained as collected water.",
                ComponentId = Id,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["overflowKgPerSecond"] = overflowKg / dt
                }
            });
        }

        if (_filmCarryoverFraction > 0.0 && uncollected > 0.0)
        {
            carryoverRate = _filmCarryoverFraction * uncollected;
            uncollected -= carryoverRate;
            proposedFilmKg = _retainedFilmKg + uncollected * dt;
            if (_maximumRetainedFilmKg > 0.0)
            {
                proposedFilmKg = Math.Min(proposedFilmKg, _maximumRetainedFilmKg);
            }

            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "CONDENSER.FILM_CARRYOVER",
                Severity = DiagnosticSeverity.Warning,
                Message = "A fraction of uncollected condensate was carried over with the outlet air stream.",
                ComponentId = Id,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["carryoverKgPerSecond"] = carryoverRate
                }
            });
        }

        LastTotalCoolingPowerW = CoolingDemandW(inlet, outlet);
        LastCondensedWaterRateKgPerSecond = condensed;
        LastCollectedWaterRateKgPerSecond = collected;
        LastUncollectedWaterRateKgPerSecond = uncollected;
        LastEffectiveDrainageEfficiency = condensed > 0.0 ? collected / condensed : _drainageEfficiency;
        LastCarryoverWaterRateKgPerSecond = carryoverRate;
        LastRetainedFilmKg = proposedFilmKg;

        if (uncollected > 1e-12)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "CONDENSER.DRAINAGE_LOSS",
                Severity = DiagnosticSeverity.Information,
                Message = "Uncollected condensate retained as drainage loss/film.",
                ComponentId = Id,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["uncollectedKgPerSecond"] = uncollected,
                    ["retainedFilmKg"] = proposedFilmKg
                }
            });
        }

        // Air-side enthalpy drop is rejected to the cooling sink. Liquid streams carry mass only
        // in this MVP energy bookkeeping (latent portion is embedded in moist-air enthalpy).
        // Carryover remains in the moist-air outlet vapor inventory for MVP bookkeeping.
        var filmStorageChangeKgPerSecond = dt > 0.0
            ? (proposedFilmKg - _retainedFilmKg) / dt
            : 0.0;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond + collected + carryoverRate,
            waterMassStorageChangeKgPerSecond: filmStorageChangeKgPerSecond,
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir
                + LastTotalCoolingPowerW,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = outlet,
                ["liquid_out"] = new LiquidWaterState
                {
                    MassFlowKgPerSecond = collected,
                    TemperatureK = Math.Min(outlet.TemperatureK, surfaceTemperatureK)
                }
            },
            ProposedInternalState = proposedFilmKg,
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ProposedInternalState is double filmKg)
        {
            _retainedFilmKg = filmKg;
        }

        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private MoistAirState BuildIdealOutlet(
        MoistAirState inlet,
        double surfaceTemperatureK,
        bool condensationPossible)
    {
        // COND-005: optional UA effectiveness overrides configured bypass for contact fraction.
        var bypass = _bypassFactor;
        if (_heatTransferUaWPerK > 0.0)
        {
                    var capacityRate = inlet.DryAirMassFlowKgPerSecond
                * (ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK
                   + inlet.HumidityRatioKgPerKgDryAir
                   * ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK);
            var effectiveness = capacityRate > 0.0
                ? 1.0 - Math.Exp(-_heatTransferUaWPerK / capacityRate)
                : 0.0;
            bypass = 1.0 - effectiveness;
        }

        var temperatureOut = bypass * inlet.TemperatureK
            + (1.0 - bypass) * surfaceTemperatureK;

        double humidityOut;
        if (!condensationPossible)
        {
            humidityOut = inlet.HumidityRatioKgPerKgDryAir;
        }
        else
        {
            var saturationPressure = _calculator.CalculateSaturationPressurePa(surfaceTemperatureK);
            if (saturationPressure >= inlet.PressurePa)
            {
                humidityOut = inlet.HumidityRatioKgPerKgDryAir;
            }
            else
            {
                var wSat = _calculator.CalculateHumidityRatio(inlet.PressurePa, saturationPressure);
                var contactedHumidity = bypass * inlet.HumidityRatioKgPerKgDryAir
                    + (1.0 - bypass) * wSat;
                // Mass-transfer effectiveness scales how much of the contacted humidity change is realized.
                humidityOut = inlet.HumidityRatioKgPerKgDryAir
                    + _massTransferEffectivenessFraction * (contactedHumidity - inlet.HumidityRatioKgPerKgDryAir);
                humidityOut = Math.Clamp(humidityOut, 0.0, inlet.HumidityRatioKgPerKgDryAir);
            }
        }

        return _calculator.CreateFromHumidityRatio(
            temperatureOut,
            inlet.PressurePa,
            humidityOut,
            inlet.DryAirMassFlowKgPerSecond);
    }

    private MoistAirState SolvePowerLimitedOutlet(
        MoistAirState inlet,
        MoistAirState idealOutlet,
        double availableCoolingPowerW)
    {
        if (availableCoolingPowerW <= 0.0)
        {
            return inlet;
        }

        var low = 0.0;
        var high = 1.0;
        MoistAirState best = inlet;

        for (var i = 0; i < 60; i++)
        {
            var alpha = 0.5 * (low + high);
            var candidate = Blend(inlet, idealOutlet, alpha);
            var demand = CoolingDemandW(inlet, candidate);
            best = candidate;

            if (Math.Abs(demand - availableCoolingPowerW) <= 1e-4)
            {
                return candidate;
            }

            if (demand > availableCoolingPowerW)
            {
                high = alpha;
            }
            else
            {
                low = alpha;
            }
        }

        return best;
    }

    private MoistAirState Blend(MoistAirState inlet, MoistAirState ideal, double alpha)
    {
        var temperatureK = inlet.TemperatureK + alpha * (ideal.TemperatureK - inlet.TemperatureK);
        var humidityRatio = inlet.HumidityRatioKgPerKgDryAir
            + alpha * (ideal.HumidityRatioKgPerKgDryAir - inlet.HumidityRatioKgPerKgDryAir);
        humidityRatio = Math.Clamp(humidityRatio, 0.0, inlet.HumidityRatioKgPerKgDryAir);

        return _calculator.CreateFromHumidityRatio(
            temperatureK,
            inlet.PressurePa,
            humidityRatio,
            inlet.DryAirMassFlowKgPerSecond);
    }

    private static double CoolingDemandW(MoistAirState inlet, MoistAirState outlet)
        => inlet.DryAirMassFlowKgPerSecond
            * (inlet.SpecificEnthalpyJPerKgDryAir - outlet.SpecificEnthalpyJPerKgDryAir);

    private ComponentStepResult Error(
        ComponentStepContext context,
        string code,
        string message,
        string portId)
        => new()
        {
            Diagnostics =
            [
                new SimulationDiagnostic
                {
                    Code = code,
                    Severity = DiagnosticSeverity.Error,
                    Message = message,
                    ComponentId = Id,
                    PortId = portId,
                    StepIndex = context.Simulation.StepIndex
                }
            ],
            Balance = ConservationBalance.Empty
        };
}
