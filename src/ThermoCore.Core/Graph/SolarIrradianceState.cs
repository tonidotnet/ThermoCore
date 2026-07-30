namespace ThermoCore.Core.Graph;

/// <summary>Plane-of-array irradiance for solar ports.</summary>
public sealed record SolarIrradianceState
{
    public required double IrradianceWPerM2 { get; init; }
}
