namespace ThermoCore.Core.Calibration;

/// <summary>Bounded scalar parameter eligible for calibration fitting (CAL-006).</summary>
public sealed record CalibratableParameter
{
    public required string Id { get; init; }

    public required double InitialValue { get; init; }

    public required double LowerBound { get; init; }

    public required double UpperBound { get; init; }

    public CalibratableParameter Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        if (double.IsNaN(InitialValue) || double.IsInfinity(InitialValue)
            || double.IsNaN(LowerBound) || double.IsInfinity(LowerBound)
            || double.IsNaN(UpperBound) || double.IsInfinity(UpperBound))
        {
            throw new ArgumentException($"Parameter '{Id}' bounds/initial value must be finite.");
        }

        if (LowerBound > UpperBound)
        {
            throw new ArgumentException($"Parameter '{Id}' has LowerBound greater than UpperBound.");
        }

        if (InitialValue < LowerBound || InitialValue > UpperBound)
        {
            throw new ArgumentException($"Parameter '{Id}' initial value is outside bounds.");
        }

        return this;
    }
}
