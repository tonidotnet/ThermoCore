using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.App2.SolarAirHeater;

/// <summary>Builds and runs the solar air heater MVP topology (APP2-005).</summary>
public sealed class SolarAirHeaterSimulationRunner
{
    private readonly SolarAirHeaterGraphBuilder _graphBuilder;
    private readonly ISimulationEngine _engine;

    public SolarAirHeaterSimulationRunner(
        SolarAirHeaterGraphBuilder? graphBuilder = null,
        ISimulationEngine? engine = null)
    {
        _graphBuilder = graphBuilder ?? new SolarAirHeaterGraphBuilder();
        _engine = engine ?? new SimulationEngine();
    }

    public SolarAirHeaterRunResult Run(
        SolarAirHeaterConfiguration configuration,
        TimeSpan? duration = null,
        TimeSpan? timeStep = null,
        DateTimeOffset? startTimeUtc = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var built = _graphBuilder.Build(configuration);
        var engineResult = _engine.Run(
            new SimulationRequest
            {
                Graph = built.Graph,
                StartTimeUtc = startTimeUtc ?? DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                Duration = duration ?? TimeSpan.FromSeconds(1),
                TimeStep = timeStep ?? TimeSpan.FromSeconds(1)
            },
            cancellationToken);

        var ambientK = configuration.AmbientTemperatureK;
        var exhaustK = ambientK;
        var usefulW = 0.0;
        if (engineResult.Steps.Count > 0)
        {
            var last = engineResult.Steps[^1];
            if (last.PortStates.TryGetValue($"{SolarAirHeaterTopologyIds.Collector}.outlet", out var outletRaw)
                && outletRaw is MoistAirState outlet)
            {
                exhaustK = outlet.TemperatureK;
            }

            if (built.Graph.Components.FirstOrDefault(c => c.Id == SolarAirHeaterTopologyIds.Collector)
                is ConstantEfficiencySolarCollectorComponent collector)
            {
                usefulW = collector.LastUsefulHeatW;
            }
        }

        var incidentW = configuration.SolarIrradianceWPerM2 * configuration.CollectorApertureAreaM2;
        return new SolarAirHeaterRunResult
        {
            BuiltSystem = built,
            EngineResult = engineResult,
            ExhaustTemperatureK = exhaustK,
            TemperatureRiseK = exhaustK - ambientK,
            UsefulHeatW = usefulW,
            IncidentSolarPowerW = incidentW
        };
    }
}
