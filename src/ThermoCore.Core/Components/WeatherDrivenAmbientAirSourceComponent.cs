using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Environment;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>Ambient moist-air source driven by <see cref="IWeatherProvider"/>.</summary>
public sealed class WeatherDrivenAmbientAirSourceComponent : ISimulationComponent
{
    private readonly IWeatherProvider _weatherProvider;
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _dryAirMassFlowKgPerSecond;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public WeatherDrivenAmbientAirSourceComponent(
        string id,
        IWeatherProvider weatherProvider,
        double dryAirMassFlowKgPerSecond,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(weatherProvider);
        FiniteNumber.RequirePositive(dryAirMassFlowKgPerSecond, nameof(dryAirMassFlowKgPerSecond));
        Id = id;
        _weatherProvider = weatherProvider;
        _dryAirMassFlowKgPerSecond = dryAirMassFlowKgPerSecond;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var timestamp = context.Simulation.SimulationStart + context.Simulation.ElapsedTime;
        var weather = _weatherProvider.GetState(timestamp);
        var outletState = _calculator.CreateFromRelativeHumidity(
            weather.AmbientTemperatureK,
            weather.AbsolutePressurePa,
            weather.RelativeHumidityFraction,
            _dryAirMassFlowKgPerSecond);

        var dt = context.Simulation.TimeStep;
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: outletState.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outletState.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: outletState.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: outletState.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: outletState.DryAirMassFlowKgPerSecond * outletState.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: outletState.DryAirMassFlowKgPerSecond * outletState.SpecificEnthalpyJPerKgDryAir,
            storedEnergyChangeW: 0.0,
            timeStep: dt);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = outletState
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
