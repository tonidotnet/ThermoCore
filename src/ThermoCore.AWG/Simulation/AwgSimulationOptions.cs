using ThermoCore.AWG.Control;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Balances;
using ThermoCore.Core.Environment;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Simulation;

/// <summary>Timing and execution options for an AWG simulation run (APP-003).</summary>
public sealed record AwgSimulationOptions
{
    public required DateTimeOffset StartTimeUtc { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public IWeatherProvider? WeatherProvider { get; init; }

    /// <summary>When true, runs <see cref="RuleBasedAwgController"/> each timestep.</summary>
    public bool EnableController { get; init; }

    public AwgControlParameters? ControlParameters { get; init; }

    public AwgOperatingMode InitialControllerMode { get; init; } = AwgOperatingMode.Off;

    /// <summary>Optional per-run conservation tolerances (defaults to engine defaults).</summary>
    public BalanceTolerance? BalanceTolerance { get; init; }

    public AwgSimulationOptions Validate()
    {
        FiniteNumber.RequirePositive(Duration.TotalSeconds, nameof(Duration));
        FiniteNumber.RequirePositive(TimeStep.TotalSeconds, nameof(TimeStep));
        if (TimeStep > Duration)
        {
            throw new ArgumentException("Time step cannot exceed duration.", nameof(TimeStep));
        }

        ControlParameters?.Validate();
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
