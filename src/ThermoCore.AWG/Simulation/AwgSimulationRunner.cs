using ThermoCore.AWG.Topology;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Simulation;

/// <summary>Timing and execution options for an AWG simulation run (APP-003).</summary>
public sealed record AwgSimulationOptions
{
    public required DateTimeOffset StartTimeUtc { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public AwgSimulationOptions Validate()
    {
        FiniteNumber.RequirePositive(Duration.TotalSeconds, nameof(Duration));
        FiniteNumber.RequirePositive(TimeStep.TotalSeconds, nameof(TimeStep));
        if (TimeStep > Duration)
        {
            throw new ArgumentException("Time step cannot exceed duration.", nameof(TimeStep));
        }

        return this;
    }

    public static AwgSimulationOptions CreateDefault(TimeSpan? duration = null, TimeSpan? timeStep = null)
        => new AwgSimulationOptions
        {
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = duration ?? TimeSpan.FromSeconds(60),
            TimeStep = timeStep ?? TimeSpan.FromSeconds(1)
        }.Validate();
}

/// <summary>Result of an AWG-hosted simulation run.</summary>
public sealed record AwgSimulationRunResult
{
    public required AwgBuiltSystem BuiltSystem { get; init; }

    public required AwgSimulationOptions Options { get; init; }

    public required SimulationRunResult EngineResult { get; init; }

    public required AwgRunSummary Summary { get; init; }
}

/// <summary>Human- and machine-readable summary of an AWG run (APP-004).</summary>
public sealed record AwgRunSummary
{
    public required bool Succeeded { get; init; }

    public required string TopologyId { get; init; }

    public required string TopologyVersion { get; init; }

    public required string GraphFingerprint { get; init; }

    public required int ComponentCount { get; init; }

    public required int ConnectionCount { get; init; }

    public required int CompletedSteps { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required double AggregatedDryAirResidualKg { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public required IReadOnlyDictionary<string, double?> FinalMoistAirTemperaturesC { get; init; }

    public required IReadOnlyDictionary<string, double?> FinalHumidityRatiosKgPerKg { get; init; }

    public double? FinalBusPowerW { get; init; }

    public double? FinalCurtailedPowerW { get; init; }
}

/// <summary>Builds and executes an AWG V3 graph from configuration.</summary>
public sealed class AwgSimulationRunner
{
    private readonly IAwgSystemGraphBuilder _graphBuilder;

    public AwgSimulationRunner(IAwgSystemGraphBuilder? graphBuilder = null)
    {
        _graphBuilder = graphBuilder ?? new AwgV3SystemGraphBuilder();
    }

    public AwgSimulationRunResult Run(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        AwgSimulationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var built = _graphBuilder.Build(configuration, initialState);
        var engineResult = new AcyclicSimulationEngine().Run(
            new SimulationRequest
            {
                Graph = built.Graph,
                StartTimeUtc = options.StartTimeUtc,
                Duration = options.Duration,
                TimeStep = options.TimeStep
            },
            cancellationToken);

        var summary = AwgRunSummaryBuilder.Build(built, options, engineResult);
        return new AwgSimulationRunResult
        {
            BuiltSystem = built,
            Options = options,
            EngineResult = engineResult,
            Summary = summary
        };
    }
}
