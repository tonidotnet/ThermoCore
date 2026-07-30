namespace ThermoCore.AWG.Optimization;

/// <summary>One discrete sweep axis over a calibratable AWG parameter id.</summary>
public sealed record AwgParameterSweepAxis
{
    public required string ParameterId { get; init; }

    public required IReadOnlyList<double> Values { get; init; }

    public AwgParameterSweepAxis Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ParameterId);
        ArgumentNullException.ThrowIfNull(Values);
        if (Values.Count == 0)
        {
            throw new ArgumentException("Sweep axis requires at least one value.", nameof(Values));
        }

        if (Values.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
        {
            throw new ArgumentException("Sweep axis values must be finite.", nameof(Values));
        }

        return this;
    }
}
