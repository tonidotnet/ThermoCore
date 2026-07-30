using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>Water-tank capacity and thermal defaults for AWG-014.</summary>
public sealed record AwgWaterTankParameters
{
    public required double CapacityKg { get; init; }

    public double InitialTemperatureK { get; init; } = 298.15;

    public AwgWaterTankParameters Validate()
    {
        FiniteNumber.RequirePositive(CapacityKg, nameof(CapacityKg));
        FiniteNumber.RequirePositive(InitialTemperatureK, nameof(InitialTemperatureK));
        return this;
    }
}
