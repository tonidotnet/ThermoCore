using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Numerics;

/// <summary>
/// Central tolerance model for approximate equality and solver convergence
/// (docs/02_Mathematics/25_NumericalMethods.md).
/// </summary>
public sealed record NumericalTolerances
{
    public double Absolute { get; init; } = 1e-10;

    public double Relative { get; init; } = 1e-7;

    public double TemperatureK { get; init; } = 1e-4;

    public double PressurePa { get; init; } = 0.1;

    public double MassKg { get; init; } = 1e-9;

    public double MassFlowKgPerSecond { get; init; } = 1e-9;

    public double EnergyJ { get; init; } = 1e-5;

    public double PowerW { get; init; } = 1e-5;

    public int MaximumIterations { get; init; } = 100;

    public static NumericalTolerances Default { get; } = new();

    public NumericalTolerances Validate()
    {
        FiniteNumber.RequirePositive(Absolute, nameof(Absolute));
        FiniteNumber.RequirePositive(Relative, nameof(Relative));
        FiniteNumber.RequirePositive(TemperatureK, nameof(TemperatureK));
        FiniteNumber.RequirePositive(PressurePa, nameof(PressurePa));
        FiniteNumber.RequirePositive(MassKg, nameof(MassKg));
        FiniteNumber.RequirePositive(MassFlowKgPerSecond, nameof(MassFlowKgPerSecond));
        FiniteNumber.RequirePositive(EnergyJ, nameof(EnergyJ));
        FiniteNumber.RequirePositive(PowerW, nameof(PowerW));

        if (MaximumIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumIterations),
                MaximumIterations,
                "MaximumIterations must be positive.");
        }

        return this;
    }
}
