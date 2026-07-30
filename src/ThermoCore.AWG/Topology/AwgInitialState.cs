using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Components.Power;

namespace ThermoCore.AWG.Topology;

/// <summary>Initial physical and control state for an AWG simulation.</summary>
public sealed record AwgInitialState
{
    public required double SilicaGelLoadingKgPerKg { get; init; }

    public required double SilicaGelTemperatureK { get; init; }

    public required double SolarCollectorAbsorberTemperatureK { get; init; }

    public required double BatteryStoredEnergyJ { get; init; }

    public double WaterTankContentKg { get; init; }

    public double RecirculationFraction { get; init; }

    public string ControllerMode { get; init; } = "Off";

    public AwgInitialState Validate(AwgSystemConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        Core.Validation.FiniteNumber.RequireNonNegative(SilicaGelLoadingKgPerKg, nameof(SilicaGelLoadingKgPerKg));
        Core.Validation.FiniteNumber.RequirePositive(SilicaGelTemperatureK, nameof(SilicaGelTemperatureK));
        Core.Validation.FiniteNumber.RequirePositive(SolarCollectorAbsorberTemperatureK, nameof(SolarCollectorAbsorberTemperatureK));
        Core.Validation.FiniteNumber.RequireNonNegative(BatteryStoredEnergyJ, nameof(BatteryStoredEnergyJ));
        Core.Validation.FiniteNumber.RequireNonNegative(WaterTankContentKg, nameof(WaterTankContentKg));
        Core.Validation.FiniteNumber.Require(RecirculationFraction, nameof(RecirculationFraction));

        if (SilicaGelLoadingKgPerKg > configuration.SilicaGel.MaximumWaterLoadingKgPerKgDryAdsorbent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SilicaGelLoadingKgPerKg),
                "Initial silica-gel loading exceeds maximum capacity.");
        }

        if (SilicaGelLoadingKgPerKg < configuration.SilicaGel.MinimumRegeneratedLoadingKgPerKgDryAdsorbent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SilicaGelLoadingKgPerKg),
                "Initial silica-gel loading is below minimum regenerated loading.");
        }

        if (BatteryStoredEnergyJ > configuration.Battery.NominalCapacityJ)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BatteryStoredEnergyJ),
                "Initial battery energy exceeds nominal capacity.");
        }

        if (RecirculationFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(RecirculationFraction));
        }

        if (!configuration.Topology.EnableRecirculation && RecirculationFraction > 0.0)
        {
            throw new ArgumentException(
                "Initial recirculation fraction must be zero when recirculation is disabled.");
        }

        return this;
    }
}
