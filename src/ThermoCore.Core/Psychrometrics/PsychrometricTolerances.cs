namespace ThermoCore.Core.Psychrometrics;

/// <summary>
/// Tolerances for psychrometric calculations (docs/02_Mathematics/05_Psychrometrics.md §51).
/// </summary>
public sealed record PsychrometricTolerances
{
    public double TemperatureK { get; init; } = 1e-4;

    public double PressurePa { get; init; } = 0.1;

    public double RelativeHumidityFraction { get; init; } = 1e-8;

    public double HumidityRatioKgPerKgDryAir { get; init; } = 1e-10;

    public int MaximumRootIterations { get; init; } = 100;

    public static PsychrometricTolerances Default { get; } = new();
}
