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
    /// Total cold-side thermal resistance between process/load temperature and module cold face
    /// (docs/03_Components/08_Peltier.md §23–§26 / TEC-004).
    /// </summary>
    public double ColdSideThermalResistanceKPerW { get; init; }

    /// <summary>
    /// Total hot-side thermal resistance between module hot face and heat-sink fluid
    /// (docs/03_Components/08_Peltier.md §23–§24 / TEC-004).
    /// </summary>
    public double HotSideThermalResistanceKPerW { get; init; }

    /// <summary>When cooling COP falls below this threshold, emit a diagnostic (TEC-007).</summary>
    public double MinimumUsefulCoolingCop { get; init; }

    public double MaximumAllowedColdSideHeatFluxWPerM2 { get; init; }

    public double ActiveColdSideAreaM2 { get; init; }

    /// <summary>
    /// When true, overtemperature / undertemperature trips zero the electrical drive
    /// (docs/03_Components/08_Peltier.md §65 / TEC-007).
    /// </summary>
    public bool EnableProtectionShutdown { get; init; } = true;

    public double HotSideThermalResistanceWarningKPerW { get; init; } = 2.0;

    public double ColdSideThermalResistanceWarningKPerW { get; init; } = 2.0;

    /// <summary>
    /// Effective cold-side thermal capacity (TEC-005). Zero keeps algebraic steady-state faces.
    /// </summary>
    public double EffectiveColdSideThermalCapacityJPerK { get; init; }

    /// <summary>
    /// Effective hot-side thermal capacity (TEC-005). Zero keeps algebraic steady-state faces.
    /// </summary>
    public double EffectiveHotSideThermalCapacityJPerK { get; init; }

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
            MinimumColdSideTemperatureK = 250.0,
            ColdSideThermalResistanceKPerW = 0.0,
            HotSideThermalResistanceKPerW = 0.0,
            MinimumUsefulCoolingCop = 0.1,
            MaximumAllowedColdSideHeatFluxWPerM2 = 0.0,
            ActiveColdSideAreaM2 = 0.0,
            EnableProtectionShutdown = true,
            EffectiveColdSideThermalCapacityJPerK = 0.0,
            EffectiveHotSideThermalCapacityJPerK = 0.0
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
        FiniteNumber.RequireNonNegative(ColdSideThermalResistanceKPerW, nameof(ColdSideThermalResistanceKPerW));
        FiniteNumber.RequireNonNegative(HotSideThermalResistanceKPerW, nameof(HotSideThermalResistanceKPerW));
        FiniteNumber.RequireNonNegative(MinimumUsefulCoolingCop, nameof(MinimumUsefulCoolingCop));
        FiniteNumber.RequireNonNegative(MaximumAllowedColdSideHeatFluxWPerM2, nameof(MaximumAllowedColdSideHeatFluxWPerM2));
        FiniteNumber.RequireNonNegative(ActiveColdSideAreaM2, nameof(ActiveColdSideAreaM2));
        FiniteNumber.RequireNonNegative(HotSideThermalResistanceWarningKPerW, nameof(HotSideThermalResistanceWarningKPerW));
        FiniteNumber.RequireNonNegative(ColdSideThermalResistanceWarningKPerW, nameof(ColdSideThermalResistanceWarningKPerW));
        FiniteNumber.RequireNonNegative(EffectiveColdSideThermalCapacityJPerK, nameof(EffectiveColdSideThermalCapacityJPerK));
        FiniteNumber.RequireNonNegative(EffectiveHotSideThermalCapacityJPerK, nameof(EffectiveHotSideThermalCapacityJPerK));

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
