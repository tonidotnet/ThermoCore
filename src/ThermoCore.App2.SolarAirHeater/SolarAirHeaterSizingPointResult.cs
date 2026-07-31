namespace ThermoCore.App2.SolarAirHeater;

/// <summary>One evaluated sizing point for APP2-006.</summary>
public sealed record SolarAirHeaterSizingPointResult
{
    public required double ApertureAreaM2 { get; init; }

    public required double DryAirMassFlowKgPerSecond { get; init; }

    public required double SolarIrradianceWPerM2 { get; init; }

    public required bool Succeeded { get; init; }

    public required double TemperatureRiseK { get; init; }

    public required double UsefulHeatW { get; init; }

    public required double SolarUtilizationFraction { get; init; }

    public string? FailureMessage { get; init; }
}
