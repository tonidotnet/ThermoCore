using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// Steady-state analytical thermoelectric coefficients
/// (docs/03_Components/08_Peltier.md §7, §9, TEC-002).
/// </summary>
public sealed record AnalyticalPeltierParameters
{
    public required double SeebeckCoefficientVPerK { get; init; }

    public required double ElectricalResistanceOhm { get; init; }

    public required double ThermalConductanceWPerK { get; init; }

    public required double MaximumCurrentA { get; init; }

    public double MaximumVoltageV { get; init; } = double.PositiveInfinity;

    public double MaximumElectricalPowerW { get; init; } = double.PositiveInfinity;

    public double? MaximumTemperatureDifferenceK { get; init; }

    public double MaximumHotSideTemperatureK { get; init; } = 400.0;

    public double MinimumColdSideTemperatureK { get; init; } = 230.0;

    public bool AllowReverseCurrent { get; init; }

    /// <summary>
    /// Provisional engineering defaults suitable for unit tests and early AWG sizing.
    /// Replace with datasheet-calibrated values before predictive use.
    /// </summary>
    public static AnalyticalPeltierParameters CreateProvisionalEngineeringDefaults()
        => new()
        {
            SeebeckCoefficientVPerK = 0.05,
            ElectricalResistanceOhm = 2.0,
            ThermalConductanceWPerK = 0.5,
            MaximumCurrentA = 6.0,
            MaximumVoltageV = 15.0,
            MaximumElectricalPowerW = 60.0,
            MaximumTemperatureDifferenceK = 70.0,
            MaximumHotSideTemperatureK = 360.0,
            MinimumColdSideTemperatureK = 250.0
        };

    public AnalyticalPeltierParameters Validate()
    {
        FiniteNumber.RequirePositive(SeebeckCoefficientVPerK, nameof(SeebeckCoefficientVPerK));
        FiniteNumber.RequirePositive(ElectricalResistanceOhm, nameof(ElectricalResistanceOhm));
        FiniteNumber.RequireNonNegative(ThermalConductanceWPerK, nameof(ThermalConductanceWPerK));
        FiniteNumber.RequirePositive(MaximumCurrentA, nameof(MaximumCurrentA));
        FiniteNumber.RequirePositive(MaximumVoltageV, nameof(MaximumVoltageV));
        FiniteNumber.RequirePositive(MaximumElectricalPowerW, nameof(MaximumElectricalPowerW));
        FiniteNumber.RequirePositive(MaximumHotSideTemperatureK, nameof(MaximumHotSideTemperatureK));
        FiniteNumber.RequirePositive(MinimumColdSideTemperatureK, nameof(MinimumColdSideTemperatureK));

        if (MaximumTemperatureDifferenceK is { } maxDelta)
        {
            FiniteNumber.RequirePositive(maxDelta, nameof(MaximumTemperatureDifferenceK));
        }

        if (MinimumColdSideTemperatureK >= MaximumHotSideTemperatureK)
        {
            throw new ArgumentException(
                "Minimum cold-side temperature must be lower than maximum hot-side temperature.");
        }

        return this;
    }
}
