using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

/// <summary>Observed system inputs for one controller evaluation step.</summary>
public sealed record AwgSystemObservation
{
    public required DateTimeOffset SimulationTimeUtc { get; init; }

    public required double AmbientTemperatureK { get; init; }

    public required double AmbientRelativeHumidityFraction { get; init; }

    public required double AmbientVaporPressurePa { get; init; }

    public required double SolarIrradianceWPerSquareMeter { get; init; }

    public required double BatteryStateOfChargeFraction { get; init; }

    public required double AvailableElectricalPowerW { get; init; }

    public required double SilicaGelLoadingKgPerKg { get; init; }

    public required double SilicaGelTemperatureK { get; init; }

    /// <summary>
    /// Equilibrium loading at current bed conditions, supplied by the silica-gel model.
    /// The controller does not evaluate isotherms.
    /// </summary>
    public required double SilicaGelEquilibriumLoadingKgPerKg { get; init; }

    public required double CondenserSurfaceTemperatureK { get; init; }

    public required double InletDewPointTemperatureK { get; init; }

    public required double CondenserInletDewPointTemperatureK { get; init; }

    public required double PeltierHotSideTemperatureK { get; init; }

    public required double PeltierColdSideTemperatureK { get; init; }

    public required double CollectorAbsorberTemperatureK { get; init; }

    public required double ProcessDryAirMassFlowKgPerSecond { get; init; }

    public required double WaterTankLevelFraction { get; init; }

    public required bool FanOperatingPointValid { get; init; }

    public required IReadOnlyCollection<SimulationDiagnostic> ComponentDiagnostics { get; init; }

    public AwgSystemObservation Validate()
    {
        FiniteNumber.RequirePositive(AmbientTemperatureK, nameof(AmbientTemperatureK));
        FiniteNumber.Require(AmbientRelativeHumidityFraction, nameof(AmbientRelativeHumidityFraction));
        FiniteNumber.RequireNonNegative(AmbientVaporPressurePa, nameof(AmbientVaporPressurePa));
        FiniteNumber.RequireNonNegative(SolarIrradianceWPerSquareMeter, nameof(SolarIrradianceWPerSquareMeter));
        FiniteNumber.Require(BatteryStateOfChargeFraction, nameof(BatteryStateOfChargeFraction));
        FiniteNumber.RequireNonNegative(AvailableElectricalPowerW, nameof(AvailableElectricalPowerW));
        FiniteNumber.RequireNonNegative(SilicaGelLoadingKgPerKg, nameof(SilicaGelLoadingKgPerKg));
        FiniteNumber.RequirePositive(SilicaGelTemperatureK, nameof(SilicaGelTemperatureK));
        FiniteNumber.RequireNonNegative(SilicaGelEquilibriumLoadingKgPerKg, nameof(SilicaGelEquilibriumLoadingKgPerKg));
        FiniteNumber.RequirePositive(CondenserSurfaceTemperatureK, nameof(CondenserSurfaceTemperatureK));
        FiniteNumber.RequirePositive(InletDewPointTemperatureK, nameof(InletDewPointTemperatureK));
        FiniteNumber.RequirePositive(CondenserInletDewPointTemperatureK, nameof(CondenserInletDewPointTemperatureK));
        FiniteNumber.RequirePositive(PeltierHotSideTemperatureK, nameof(PeltierHotSideTemperatureK));
        FiniteNumber.RequirePositive(PeltierColdSideTemperatureK, nameof(PeltierColdSideTemperatureK));
        FiniteNumber.RequirePositive(CollectorAbsorberTemperatureK, nameof(CollectorAbsorberTemperatureK));
        FiniteNumber.RequireNonNegative(ProcessDryAirMassFlowKgPerSecond, nameof(ProcessDryAirMassFlowKgPerSecond));
        FiniteNumber.Require(WaterTankLevelFraction, nameof(WaterTankLevelFraction));
        ArgumentNullException.ThrowIfNull(ComponentDiagnostics);

        if (AmbientRelativeHumidityFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(AmbientRelativeHumidityFraction));
        }

        if (BatteryStateOfChargeFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(BatteryStateOfChargeFraction));
        }

        if (WaterTankLevelFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(WaterTankLevelFraction));
        }

        return this;
    }
}
