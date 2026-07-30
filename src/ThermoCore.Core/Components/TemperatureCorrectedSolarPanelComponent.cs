using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

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
