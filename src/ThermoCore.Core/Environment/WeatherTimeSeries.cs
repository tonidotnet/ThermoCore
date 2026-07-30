namespace ThermoCore.Core.Environment;

/// <summary>Ordered weather samples with source metadata.</summary>
public sealed record WeatherTimeSeries
{
    public required IReadOnlyList<WeatherState> States { get; init; }

    public required WeatherSourceMetadata Metadata { get; init; }

    public WeatherTimeSeries Validate()
    {
        ArgumentNullException.ThrowIfNull(States);
        ArgumentNullException.ThrowIfNull(Metadata);
        if (States.Count == 0)
        {
            throw new ArgumentException("Weather series must contain at least one state.", nameof(States));
        }

        DateTimeOffset? previous = null;
        foreach (var state in States)
        {
            state.Validate();
            if (previous is { } prior && state.TimestampUtc <= prior)
            {
                throw new ArgumentException(
                    "Weather timestamps must be strictly increasing.",
                    nameof(States));
            }

            previous = state.TimestampUtc;
        }

        return this;
    }
}
