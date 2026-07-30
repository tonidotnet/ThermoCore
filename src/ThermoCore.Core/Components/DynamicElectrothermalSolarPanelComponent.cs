using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

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
