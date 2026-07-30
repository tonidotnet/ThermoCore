using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Environment;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.Core.Components;

/// <summary>Solar irradiance source driven by <see cref="IWeatherProvider"/> GHI.</summary>
public sealed class WeatherDrivenSolarRadiationSourceComponent : ISimulationComponent
{
    private readonly IWeatherProvider _weatherProvider;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public WeatherDrivenSolarRadiationSourceComponent(string id, IWeatherProvider weatherProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(weatherProvider);
        Id = id;
        _weatherProvider = weatherProvider;
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.SolarRadiation)
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
        var state = new SolarIrradianceState
        {
            IrradianceWPerM2 = weather.GlobalHorizontalIrradianceWPerM2
        };

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = state
            },
            Balance = ConservationBalance.Empty
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
