namespace ThermoCore.Core.Calibration;

/// <summary>Request for bounded coordinate-descent parameter fitting.</summary>
public sealed record ParameterFittingRequest
{
    public required IReadOnlyList<CalibratableParameter> Parameters { get; init; }

    /// <summary>Maps candidate parameter values to a scalar objective to minimize (e.g. overall RMSE).</summary>
    public required Func<IReadOnlyDictionary<string, double>, double> Objective { get; init; }

    public int MaximumPasses { get; init; } = 4;

    public int MaximumEvaluationsPerParameter { get; init; } = 16;

    public double RelativeTolerance { get; init; } = 1e-4;

    public ParameterFittingRequest Validate()
    {
        ArgumentNullException.ThrowIfNull(Parameters);
        ArgumentNullException.ThrowIfNull(Objective);
        if (Parameters.Count == 0)
        {
            throw new ArgumentException("At least one parameter is required.", nameof(Parameters));
        }

        foreach (var parameter in Parameters)
        {
            parameter.Validate();
        }

        if (MaximumPasses <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumPasses));
        }

        if (MaximumEvaluationsPerParameter < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumEvaluationsPerParameter));
        }

        if (RelativeTolerance is <= 0.0 or >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(RelativeTolerance));
        }

        return this;
    }
}
