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

/// <summary>
/// Temperature-corrected PV model (PV-002 / docs/03_Components/07_SolarPanel.md §14–§16, §29).
/// </summary>
public sealed class TemperatureCorrectedSolarPanelComponent : ISimulationComponent
{
    private readonly double _ratedPowerW;
    private readonly double _referenceIrradianceWPerM2;
    private readonly double _referenceCellTemperatureK;
    private readonly double _powerTemperatureCoefficientPerK;
    private readonly double _noctCelsius;
    private readonly double _fallbackAmbientTemperatureK;
    private readonly double _mpptEfficiencyFraction;
    private readonly double _wiringEfficiencyFraction;
    private readonly double _fallbackIrradianceWPerM2;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public TemperatureCorrectedSolarPanelComponent(
        string id,
        double ratedPowerW,
        double areaM2,
        double powerTemperatureCoefficientPerK,
        double referenceIrradianceWPerM2 = 1000.0,
        double referenceCellTemperatureK = 298.15,
        double noctCelsius = 45.0,
        double fallbackAmbientTemperatureK = 298.15,
        double mpptEfficiencyFraction = 1.0,
        double wiringEfficiencyFraction = 1.0,
        double fallbackIrradianceWPerM2 = 0.0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequirePositive(ratedPowerW, nameof(ratedPowerW));
        FiniteNumber.RequirePositive(areaM2, nameof(areaM2));
        FiniteNumber.Require(powerTemperatureCoefficientPerK, nameof(powerTemperatureCoefficientPerK));
        FiniteNumber.RequirePositive(referenceIrradianceWPerM2, nameof(referenceIrradianceWPerM2));
        FiniteNumber.RequirePositive(referenceCellTemperatureK, nameof(referenceCellTemperatureK));
        FiniteNumber.Require(noctCelsius, nameof(noctCelsius));
        FiniteNumber.RequirePositive(fallbackAmbientTemperatureK, nameof(fallbackAmbientTemperatureK));
        FiniteNumber.RequirePositive(mpptEfficiencyFraction, nameof(mpptEfficiencyFraction));
        FiniteNumber.RequirePositive(wiringEfficiencyFraction, nameof(wiringEfficiencyFraction));
        if (mpptEfficiencyFraction > 1.0 || wiringEfficiencyFraction > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                mpptEfficiencyFraction > 1.0 ? nameof(mpptEfficiencyFraction) : nameof(wiringEfficiencyFraction),
                "Efficiency fractions must be in (0, 1].");
        }

        FiniteNumber.RequireNonNegative(fallbackIrradianceWPerM2, nameof(fallbackIrradianceWPerM2));

        Id = id;
        _ratedPowerW = ratedPowerW;
        _powerTemperatureCoefficientPerK = powerTemperatureCoefficientPerK;
        _referenceIrradianceWPerM2 = referenceIrradianceWPerM2;
        _referenceCellTemperatureK = referenceCellTemperatureK;
        _noctCelsius = noctCelsius;
        _fallbackAmbientTemperatureK = fallbackAmbientTemperatureK;
        _mpptEfficiencyFraction = mpptEfficiencyFraction;
        _wiringEfficiencyFraction = wiringEfficiencyFraction;
        _fallbackIrradianceWPerM2 = fallbackIrradianceWPerM2;
        Ports =
        [
            new PhysicalPort("solar", id, PortDirection.Input, PhysicalDomain.SolarRadiation, isRequired: false),
            new PhysicalPort("electrical", id, PortDirection.Output, PhysicalDomain.Electricity)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastElectricalPowerW { get; private set; }

    public double LastCellTemperatureK { get; private set; }

    public double LastRawDcPowerW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastElectricalPowerW = 0.0;
        LastCellTemperatureK = _fallbackAmbientTemperatureK;
        LastRawDcPowerW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var irradiance = _fallbackIrradianceWPerM2;
        if (context.InputStates.TryGetValue("solar", out var solarRaw)
            && solarRaw is SolarIrradianceState solar)
        {
            FiniteNumber.RequireNonNegative(solar.IrradianceWPerM2, nameof(solar.IrradianceWPerM2));
            irradiance = solar.IrradianceWPerM2;
        }

        var ambientC = UnitConversions.KelvinToCelsius(_fallbackAmbientTemperatureK);
        var cellC = ambientC + (_noctCelsius - 20.0) / 800.0 * irradiance;
        LastCellTemperatureK = UnitConversions.CelsiusToKelvin(cellC);

        var temperatureFactor = 1.0
            + _powerTemperatureCoefficientPerK * (LastCellTemperatureK - _referenceCellTemperatureK);
        if (temperatureFactor < 0.0)
        {
            temperatureFactor = 0.0;
        }

        LastRawDcPowerW = _ratedPowerW
            * (irradiance / _referenceIrradianceWPerM2)
            * temperatureFactor;
        LastElectricalPowerW = LastRawDcPowerW * _mpptEfficiencyFraction * _wiringEfficiencyFraction;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.0,
            dryAirMassOutputKgPerSecond: 0.0,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: 0.0,
            waterMassOutputKgPerSecond: 0.0,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: LastElectricalPowerW,
            energyOutputW: LastElectricalPowerW,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["electrical"] = new ElectricalPowerState { PowerW = LastElectricalPowerW }
            },
            Balance = balance
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}

/// <summary>
/// Dynamic electrothermal PV with optional rear-air cooling and channel pressure drop
/// (PV-003 / PV-004 / PV-005).
/// C_pv dT/dt = P_abs − P_elec − Q_env − Q_rear.
/// </summary>
public sealed class DynamicElectrothermalSolarPanelComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _ratedPowerW;
    private readonly double _areaM2;
    private readonly double _opticalAbsorptanceFraction;
    private readonly double _effectiveThermalCapacityJPerK;
    private readonly double _environmentalLossUaWPerK;
    private readonly double _rearAirUaWPerK;
    private readonly double _powerTemperatureCoefficientPerK;
    private readonly double _referenceIrradianceWPerM2;
    private readonly double _referenceCellTemperatureK;
    private readonly double _ambientTemperatureK;
    private readonly double _mpptEfficiencyFraction;
    private readonly double _wiringEfficiencyFraction;
    private readonly double _referencePressureDropPa;
    private readonly double _referenceVolumetricFlowM3PerSecond;
    private readonly double _pressureDropExponent;
    private readonly double _fallbackIrradianceWPerM2;
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private double _cellTemperatureK;

    public DynamicElectrothermalSolarPanelComponent(
        string id,
        double ratedPowerW,
        double areaM2,
        double effectiveThermalCapacityJPerK,
        double opticalAbsorptanceFraction,
        double environmentalLossUaWPerK,
        double initialCellTemperatureK,
        double ambientTemperatureK,
        double powerTemperatureCoefficientPerK = -0.004,
        double referenceIrradianceWPerM2 = 1000.0,
        double referenceCellTemperatureK = 298.15,
        double rearAirUaWPerK = 0.0,
        double mpptEfficiencyFraction = 1.0,
        double wiringEfficiencyFraction = 1.0,
        double referencePressureDropPa = 0.0,
        double referenceVolumetricFlowM3PerSecond = 0.01,
        double pressureDropExponent = 2.0,
        double fallbackIrradianceWPerM2 = 0.0,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequirePositive(ratedPowerW, nameof(ratedPowerW));
        FiniteNumber.RequirePositive(areaM2, nameof(areaM2));
        FiniteNumber.RequirePositive(effectiveThermalCapacityJPerK, nameof(effectiveThermalCapacityJPerK));
        FiniteNumber.Require(opticalAbsorptanceFraction, nameof(opticalAbsorptanceFraction));
        if (opticalAbsorptanceFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(opticalAbsorptanceFraction));
        }

        FiniteNumber.RequireNonNegative(environmentalLossUaWPerK, nameof(environmentalLossUaWPerK));
        FiniteNumber.RequirePositive(initialCellTemperatureK, nameof(initialCellTemperatureK));
        FiniteNumber.RequirePositive(ambientTemperatureK, nameof(ambientTemperatureK));
        FiniteNumber.Require(powerTemperatureCoefficientPerK, nameof(powerTemperatureCoefficientPerK));
        FiniteNumber.RequirePositive(referenceIrradianceWPerM2, nameof(referenceIrradianceWPerM2));
        FiniteNumber.RequirePositive(referenceCellTemperatureK, nameof(referenceCellTemperatureK));
        FiniteNumber.RequireNonNegative(rearAirUaWPerK, nameof(rearAirUaWPerK));
        FiniteNumber.RequirePositive(mpptEfficiencyFraction, nameof(mpptEfficiencyFraction));
        FiniteNumber.RequirePositive(wiringEfficiencyFraction, nameof(wiringEfficiencyFraction));
        if (mpptEfficiencyFraction > 1.0 || wiringEfficiencyFraction > 1.0)
        {
            throw new ArgumentOutOfRangeException("Efficiency fractions must be in (0, 1].");
        }

        FiniteNumber.RequireNonNegative(referencePressureDropPa, nameof(referencePressureDropPa));
        FiniteNumber.RequirePositive(referenceVolumetricFlowM3PerSecond, nameof(referenceVolumetricFlowM3PerSecond));
        FiniteNumber.RequirePositive(pressureDropExponent, nameof(pressureDropExponent));
        FiniteNumber.RequireNonNegative(fallbackIrradianceWPerM2, nameof(fallbackIrradianceWPerM2));

        Id = id;
        _ratedPowerW = ratedPowerW;
        _areaM2 = areaM2;
        _opticalAbsorptanceFraction = opticalAbsorptanceFraction;
        _effectiveThermalCapacityJPerK = effectiveThermalCapacityJPerK;
        _environmentalLossUaWPerK = environmentalLossUaWPerK;
        _rearAirUaWPerK = rearAirUaWPerK;
        _powerTemperatureCoefficientPerK = powerTemperatureCoefficientPerK;
        _referenceIrradianceWPerM2 = referenceIrradianceWPerM2;
        _referenceCellTemperatureK = referenceCellTemperatureK;
        _ambientTemperatureK = ambientTemperatureK;
        _mpptEfficiencyFraction = mpptEfficiencyFraction;
        _wiringEfficiencyFraction = wiringEfficiencyFraction;
        _referencePressureDropPa = referencePressureDropPa;
        _referenceVolumetricFlowM3PerSecond = referenceVolumetricFlowM3PerSecond;
        _pressureDropExponent = pressureDropExponent;
        _fallbackIrradianceWPerM2 = fallbackIrradianceWPerM2;
        _cellTemperatureK = initialCellTemperatureK;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("solar", id, PortDirection.Input, PhysicalDomain.SolarRadiation, isRequired: false),
            new PhysicalPort("electrical", id, PortDirection.Output, PhysicalDomain.Electricity),
            new PhysicalPort("rear_air_in", id, PortDirection.Input, PhysicalDomain.MoistAir, isRequired: false),
            new PhysicalPort("rear_air_out", id, PortDirection.Output, PhysicalDomain.MoistAir, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double CellTemperatureK => _cellTemperatureK;

    public double LastAbsorbedSolarPowerW { get; private set; }

    public double LastElectricalPowerW { get; private set; }

    public double LastEnvironmentalLossW { get; private set; }

    public double LastRearAirHeatW { get; private set; }

    public double LastRearAirPressureDropPa { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastAbsorbedSolarPowerW = 0.0;
        LastElectricalPowerW = 0.0;
        LastEnvironmentalLossW = 0.0;
        LastRearAirHeatW = 0.0;
        LastRearAirPressureDropPa = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var dt = context.Simulation.TimeStep.TotalSeconds;
        if (dt <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), "Dynamic PV requires a positive timestep.");
        }

        var irradiance = _fallbackIrradianceWPerM2;
        if (context.InputStates.TryGetValue("solar", out var solarRaw)
            && solarRaw is SolarIrradianceState solar)
        {
            FiniteNumber.RequireNonNegative(solar.IrradianceWPerM2, nameof(solar.IrradianceWPerM2));
            irradiance = solar.IrradianceWPerM2;
        }

        LastAbsorbedSolarPowerW = irradiance * _areaM2 * _opticalAbsorptanceFraction;

        var temperatureFactor = 1.0
            + _powerTemperatureCoefficientPerK * (_cellTemperatureK - _referenceCellTemperatureK);
        if (temperatureFactor < 0.0)
        {
            temperatureFactor = 0.0;
        }

        var rawDc = _ratedPowerW * (irradiance / _referenceIrradianceWPerM2) * temperatureFactor;
        LastElectricalPowerW = Math.Max(0.0, rawDc * _mpptEfficiencyFraction * _wiringEfficiencyFraction);
        LastEnvironmentalLossW = _environmentalLossUaWPerK * (_cellTemperatureK - _ambientTemperatureK);

        var outputs = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["electrical"] = new ElectricalPowerState { PowerW = LastElectricalPowerW }
        };

        LastRearAirHeatW = 0.0;
        LastRearAirPressureDropPa = 0.0;
        var rearAirEnergyIn = 0.0;
        var rearAirEnergyOut = 0.0;
        var rearDryAir = 0.0;
        var rearWater = 0.0;

        if (context.InputStates.TryGetValue("rear_air_in", out var rearRaw)
            && rearRaw is MoistAirState rearIn
            && rearIn.DryAirMassFlowKgPerSecond > 0.0
            && _rearAirUaWPerK > 0.0)
        {
            var capacityRate = rearIn.DryAirMassFlowKgPerSecond
                * (ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK
                   + rearIn.HumidityRatioKgPerKgDryAir
                   * ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK);
            var effectiveness = 1.0 - Math.Exp(-_rearAirUaWPerK / capacityRate);
            LastRearAirHeatW = effectiveness * capacityRate * (_cellTemperatureK - rearIn.TemperatureK);
            var rearOutT = rearIn.TemperatureK + effectiveness * (_cellTemperatureK - rearIn.TemperatureK);
            rearOutT = Math.Clamp(rearOutT, 230.0, 450.0);

            var volumetricFlow = rearIn.DryAirMassFlowKgPerSecond * rearIn.SpecificVolumeM3PerKgDryAir;
            LastRearAirPressureDropPa = _referencePressureDropPa <= 0.0
                ? 0.0
                : _referencePressureDropPa
                  * Math.Pow(Math.Abs(volumetricFlow / _referenceVolumetricFlowM3PerSecond), _pressureDropExponent);
            var rearOutP = rearIn.PressurePa - LastRearAirPressureDropPa;
            if (rearOutP <= 0.0)
            {
                return new ComponentStepResult
                {
                    Diagnostics =
                    [
                        new SimulationDiagnostic
                        {
                            Code = "PV.NON_POSITIVE_PRESSURE",
                            Severity = DiagnosticSeverity.Error,
                            Message = "Rear-air outlet pressure would be non-positive.",
                            ComponentId = Id
                        }
                    ],
                    Balance = ConservationBalance.Empty
                };
            }

            var rearOut = _calculator.CreateFromHumidityRatio(
                rearOutT,
                rearOutP,
                rearIn.HumidityRatioKgPerKgDryAir,
                rearIn.DryAirMassFlowKgPerSecond);
            outputs["rear_air_out"] = rearOut;
            rearAirEnergyIn = rearIn.DryAirMassFlowKgPerSecond * rearIn.SpecificEnthalpyJPerKgDryAir;
            rearAirEnergyOut = rearOut.DryAirMassFlowKgPerSecond * rearOut.SpecificEnthalpyJPerKgDryAir;
            rearDryAir = rearIn.DryAirMassFlowKgPerSecond;
            rearWater = rearIn.WaterVaporMassFlowKgPerSecond;
        }

        var proposedCell = _cellTemperatureK
            + (LastAbsorbedSolarPowerW - LastElectricalPowerW - LastEnvironmentalLossW - LastRearAirHeatW)
            * dt
            / _effectiveThermalCapacityJPerK;
        proposedCell = Math.Clamp(proposedCell, 230.0, 450.0);
        var storedEnergyChangeW = _effectiveThermalCapacityJPerK * (proposedCell - _cellTemperatureK) / dt;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: rearDryAir,
            dryAirMassOutputKgPerSecond: rearDryAir,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: rearWater,
            waterMassOutputKgPerSecond: rearWater,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: LastAbsorbedSolarPowerW + rearAirEnergyIn,
            energyOutputW: LastElectricalPowerW + LastEnvironmentalLossW + rearAirEnergyOut,
            storedEnergyChangeW: storedEnergyChangeW,
            timeStep: context.Simulation.TimeStep,
            electricalPowerInputW: LastElectricalPowerW,
            electricalPowerOutputW: LastElectricalPowerW);

        return new ComponentStepResult
        {
            OutputStates = outputs,
            ProposedInternalState = proposedCell,
            Balance = balance
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ProposedInternalState is double proposed)
        {
            _cellTemperatureK = proposed;
        }

        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}
