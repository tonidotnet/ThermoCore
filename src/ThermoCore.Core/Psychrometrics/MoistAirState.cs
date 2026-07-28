namespace ThermoCore.Core.Psychrometrics;

/// <summary>
/// Immutable moist-air state. Construct only through <see cref="IPsychrometricCalculator"/>.
/// </summary>
public sealed record MoistAirState
{
    public required double TemperatureK { get; init; }

    public required double PressurePa { get; init; }

    public required double HumidityRatioKgPerKgDryAir { get; init; }

    public required double DryAirMassFlowKgPerSecond { get; init; }

    public required double VaporPressurePa { get; init; }

    public required double RelativeHumidityFraction { get; init; }

    public required double DewPointTemperatureK { get; init; }

    public required double SpecificEnthalpyJPerKgDryAir { get; init; }

    public required double SpecificVolumeM3PerKgDryAir { get; init; }

    public required double MoistAirDensityKgPerM3 { get; init; }

    public required double WaterVaporMassFlowKgPerSecond { get; init; }

    public required MoistAirPhaseState PhaseState { get; init; }
}
