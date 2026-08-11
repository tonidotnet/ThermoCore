using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// Explicit analytical TEC coefficients when a profile supplies fitted α, R, K
/// instead of (or in addition to) datasheet ratings.
/// </summary>
public sealed record TecAnalyticalCoefficientSet
{
    public required double SeebeckCoefficientVPerK { get; init; }

    public required double ElectricalResistanceOhm { get; init; }

    public required double ThermalConductanceWPerK { get; init; }

    public TecAnalyticalCoefficientSet Validate()
    {
        FiniteNumber.RequirePositive(SeebeckCoefficientVPerK, nameof(SeebeckCoefficientVPerK));
        FiniteNumber.RequirePositive(ElectricalResistanceOhm, nameof(ElectricalResistanceOhm));
        FiniteNumber.RequireNonNegative(ThermalConductanceWPerK, nameof(ThermalConductanceWPerK));
        return this;
    }
}
