using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Dynamic lumped solar collector (SC-003–SC-006).
/// Optical absorption, absorber thermal mass, wind-corrected environmental loss,
/// stagnation/overtemperature diagnostics, and optional air-path pressure drop.
/// </summary>
public sealed class DynamicLumpedSolarCollectorComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _opticalEfficiencyFraction;
    private readonly double _apertureAreaM2;
    private readonly double _incidenceAngleModifierFraction;
    private readonly double _effectiveThermalCapacityJPerK;
    private readonly double _absorberToAirUaWPerK;
    private readonly double _overallLossCoefficientWPerM2K;
    private readonly double _windSpeedMPerSecond;
    private readonly double _windLossCoefficientWPerM2KPerMps;
    private readonly double _ambientTemperatureK;
    private readonly double _maximumAllowedAbsorberTemperatureK;
    private readonly double _minimumOperatingMassFlowKgPerSecond;
    private readonly double _referencePressureDropPa;
    private readonly double _referenceVolumetricFlowM3PerSecond;
    private readonly double _pressureDropExponent;
    private readonly double _fallbackIrradianceWPerM2;
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private double _absorberTemperatureK;

    public DynamicLumpedSolarCollectorComponent(
        string id,
        double opticalEfficiencyFraction,
        double apertureAreaM2,
        double effectiveThermalCapacityJPerK,
        double absorberToAirUaWPerK,
        double overallLossCoefficientWPerM2K,
        double initialAbsorberTemperatureK,
        double ambientTemperatureK,
        double incidenceAngleModifierFraction = 1.0,
        double fallbackIrradianceWPerM2 = 0.0,
        double windSpeedMPerSecond = 0.0,
        double windLossCoefficientWPerM2KPerMps = 0.0,
        double maximumAllowedAbsorberTemperatureK = 423.15,
        double minimumOperatingMassFlowKgPerSecond = 0.0,
        double referencePressureDropPa = 0.0,
        double referenceVolumetricFlowM3PerSecond = 0.01,
        double pressureDropExponent = 2.0,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(opticalEfficiencyFraction, nameof(opticalEfficiencyFraction));
        if (opticalEfficiencyFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(opticalEfficiencyFraction));
        }

        FiniteNumber.RequirePositive(apertureAreaM2, nameof(apertureAreaM2));
        FiniteNumber.RequirePositive(effectiveThermalCapacityJPerK, nameof(effectiveThermalCapacityJPerK));
        FiniteNumber.RequireNonNegative(absorberToAirUaWPerK, nameof(absorberToAirUaWPerK));
        FiniteNumber.RequireNonNegative(overallLossCoefficientWPerM2K, nameof(overallLossCoefficientWPerM2K));
        FiniteNumber.RequirePositive(initialAbsorberTemperatureK, nameof(initialAbsorberTemperatureK));
        FiniteNumber.RequirePositive(ambientTemperatureK, nameof(ambientTemperatureK));
        FiniteNumber.Require(incidenceAngleModifierFraction, nameof(incidenceAngleModifierFraction));
        if (incidenceAngleModifierFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(incidenceAngleModifierFraction));
        }

        FiniteNumber.RequireNonNegative(fallbackIrradianceWPerM2, nameof(fallbackIrradianceWPerM2));
        FiniteNumber.RequireNonNegative(windSpeedMPerSecond, nameof(windSpeedMPerSecond));
        FiniteNumber.RequireNonNegative(windLossCoefficientWPerM2KPerMps, nameof(windLossCoefficientWPerM2KPerMps));
        FiniteNumber.RequirePositive(maximumAllowedAbsorberTemperatureK, nameof(maximumAllowedAbsorberTemperatureK));
        FiniteNumber.RequireNonNegative(minimumOperatingMassFlowKgPerSecond, nameof(minimumOperatingMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(referencePressureDropPa, nameof(referencePressureDropPa));
        FiniteNumber.RequirePositive(referenceVolumetricFlowM3PerSecond, nameof(referenceVolumetricFlowM3PerSecond));
        FiniteNumber.RequirePositive(pressureDropExponent, nameof(pressureDropExponent));

        Id = id;
        _opticalEfficiencyFraction = opticalEfficiencyFraction;
        _apertureAreaM2 = apertureAreaM2;
        _incidenceAngleModifierFraction = incidenceAngleModifierFraction;
        _effectiveThermalCapacityJPerK = effectiveThermalCapacityJPerK;
        _absorberToAirUaWPerK = absorberToAirUaWPerK;
        _overallLossCoefficientWPerM2K = overallLossCoefficientWPerM2K;
        _windSpeedMPerSecond = windSpeedMPerSecond;
        _windLossCoefficientWPerM2KPerMps = windLossCoefficientWPerM2KPerMps;
        _ambientTemperatureK = ambientTemperatureK;
        _maximumAllowedAbsorberTemperatureK = maximumAllowedAbsorberTemperatureK;
        _minimumOperatingMassFlowKgPerSecond = minimumOperatingMassFlowKgPerSecond;
        _referencePressureDropPa = referencePressureDropPa;
        _referenceVolumetricFlowM3PerSecond = referenceVolumetricFlowM3PerSecond;
        _pressureDropExponent = pressureDropExponent;
        _fallbackIrradianceWPerM2 = fallbackIrradianceWPerM2;
        _absorberTemperatureK = initialAbsorberTemperatureK;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir),
            new PhysicalPort("solar", id, PortDirection.Input, PhysicalDomain.SolarRadiation, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double AbsorberTemperatureK => _absorberTemperatureK;

    public double LastAbsorbedSolarPowerW { get; private set; }

    public double LastUsefulHeatW { get; private set; }

    public double LastEnvironmentalLossW { get; private set; }

    public double LastEffectiveLossCoefficientWPerM2K { get; private set; }

    public double LastPressureDropPa { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastAbsorbedSolarPowerW = 0.0;
        LastUsefulHeatW = 0.0;
        LastEnvironmentalLossW = 0.0;
        LastEffectiveLossCoefficientWPerM2K = 0.0;
        LastPressureDropPa = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var dt = context.Simulation.TimeStep.TotalSeconds;
        if (dt <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), "Dynamic collector requires a positive timestep.");
        }

        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return Missing("COMPONENT.MISSING_INLET", "Collector requires MoistAirState on 'inlet'.", "inlet", context);
        }

        var diagnostics = new List<SimulationDiagnostic>();
        var zeroFlow = inlet.DryAirMassFlowKgPerSecond <= 0.0;
        if (!zeroFlow
            && _minimumOperatingMassFlowKgPerSecond > 0.0
            && inlet.DryAirMassFlowKgPerSecond < _minimumOperatingMassFlowKgPerSecond)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "COLLECTOR.LOW_AIRFLOW",
                Severity = DiagnosticSeverity.Warning,
                Message = "Airflow is below the configured minimum operating mass flow.",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex
            });
        }

        var irradiance = _fallbackIrradianceWPerM2;
        if (context.InputStates.TryGetValue("solar", out var solarRaw)
            && solarRaw is SolarIrradianceState solar)
        {
            FiniteNumber.RequireNonNegative(solar.IrradianceWPerM2, nameof(solar.IrradianceWPerM2));
            irradiance = solar.IrradianceWPerM2;
        }

        LastAbsorbedSolarPowerW = irradiance
            * _apertureAreaM2
            * _opticalEfficiencyFraction
            * _incidenceAngleModifierFraction;

        // SC-004: U_L = U_L0 + k_wind · v_wind
        LastEffectiveLossCoefficientWPerM2K = _overallLossCoefficientWPerM2K
            + _windLossCoefficientWPerM2KPerMps * _windSpeedMPerSecond;
        LastEnvironmentalLossW = LastEffectiveLossCoefficientWPerM2K
            * _apertureAreaM2
            * (_absorberTemperatureK - _ambientTemperatureK);

        double effectiveness;
        MoistAirState outlet;
        if (zeroFlow)
        {
            // SC-005 stagnation: no useful air heating; absorber stores absorbed − loss.
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "COLLECTOR.STAGNATION",
                Severity = DiagnosticSeverity.Information,
                Message = "Zero airflow; absorber is in stagnation mode (no useful heat to air).",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex
            });
            LastUsefulHeatW = 0.0;
            effectiveness = 0.0;
            outlet = inlet;
            LastPressureDropPa = 0.0;
        }
        else
        {
            var capacityRate = inlet.DryAirMassFlowKgPerSecond
                * (ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK
                   + inlet.HumidityRatioKgPerKgDryAir
                   * ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK);
            effectiveness = capacityRate <= 0.0 || _absorberToAirUaWPerK <= 0.0
                ? 0.0
                : 1.0 - Math.Exp(-_absorberToAirUaWPerK / capacityRate);

            LastUsefulHeatW = effectiveness * capacityRate * (_absorberTemperatureK - inlet.TemperatureK);

            var outletTemperatureK = inlet.TemperatureK
                + effectiveness * (_absorberTemperatureK - inlet.TemperatureK);
            outletTemperatureK = Math.Clamp(outletTemperatureK, 230.0, 450.0);

            // SC-006 quadratic pressure drop on air path.
            var volumetricFlow = inlet.DryAirMassFlowKgPerSecond * inlet.SpecificVolumeM3PerKgDryAir;
            LastPressureDropPa = _referencePressureDropPa <= 0.0
                ? 0.0
                : _referencePressureDropPa
                  * Math.Pow(Math.Abs(volumetricFlow / _referenceVolumetricFlowM3PerSecond), _pressureDropExponent);
            var outletPressure = inlet.PressurePa - LastPressureDropPa;
            if (outletPressure <= 0.0)
            {
                return Missing(
                    "COLLECTOR.NON_POSITIVE_PRESSURE",
                    "Collector outlet pressure would be non-positive after pressure drop.",
                    "outlet",
                    context);
            }

            outlet = _calculator.CreateFromHumidityRatio(
                outletTemperatureK,
                outletPressure,
                inlet.HumidityRatioKgPerKgDryAir,
                inlet.DryAirMassFlowKgPerSecond);
        }

        var proposedAbsorberTemperatureK = _absorberTemperatureK
            + (LastAbsorbedSolarPowerW - LastUsefulHeatW - LastEnvironmentalLossW)
            * dt
            / _effectiveThermalCapacityJPerK;
        proposedAbsorberTemperatureK = Math.Clamp(proposedAbsorberTemperatureK, 230.0, 500.0);

        if (proposedAbsorberTemperatureK > _maximumAllowedAbsorberTemperatureK
            || _absorberTemperatureK > _maximumAllowedAbsorberTemperatureK)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "COLLECTOR.OVERTEMPERATURE",
                Severity = DiagnosticSeverity.Warning,
                Message = "Absorber temperature exceeds the configured maximum.",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex
            });
        }

        var storedEnergyChangeW = _effectiveThermalCapacityJPerK
            * (proposedAbsorberTemperatureK - _absorberTemperatureK)
            / dt;

        var dryAirIn = zeroFlow ? 0.0 : inlet.DryAirMassFlowKgPerSecond;
        var waterIn = zeroFlow ? 0.0 : inlet.WaterVaporMassFlowKgPerSecond;
        var energyStreamIn = zeroFlow
            ? 0.0
            : inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir;
        var energyStreamOut = zeroFlow
            ? 0.0
            : outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: dryAirIn,
            dryAirMassOutputKgPerSecond: dryAirIn,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: waterIn,
            waterMassOutputKgPerSecond: waterIn,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: energyStreamIn + LastAbsorbedSolarPowerW,
            energyOutputW: energyStreamOut + LastEnvironmentalLossW,
            storedEnergyChangeW: storedEnergyChangeW,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = outlet
            },
            ProposedInternalState = proposedAbsorberTemperatureK,
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ProposedInternalState is double proposed)
        {
            _absorberTemperatureK = proposed;
        }

        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private ComponentStepResult Missing(
        string code,
        string message,
        string portId,
        ComponentStepContext context)
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
