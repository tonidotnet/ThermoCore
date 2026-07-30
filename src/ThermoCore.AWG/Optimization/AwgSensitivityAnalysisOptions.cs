namespace ThermoCore.AWG.Optimization;

/// <summary>Options for one-at-a-time local sensitivity analysis (OPT-003).</summary>
public sealed record AwgSensitivityAnalysisOptions
{
    /// <summary>
    /// Relative half-step applied to each parameter baseline value (± fraction),
    /// clamped to calibratable bounds.
    /// </summary>
    public double RelativePerturbationFraction { get; init; } = 0.10;

    public AwgSensitivityAnalysisOptions Validate()
    {
        if (RelativePerturbationFraction is <= 0.0 or >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RelativePerturbationFraction),
                "Relative perturbation must be in (0, 1).");
        }

        return this;
    }
}
