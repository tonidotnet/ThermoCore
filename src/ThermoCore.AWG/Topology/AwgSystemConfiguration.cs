using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Components.Power;

namespace ThermoCore.AWG.Topology;

/// <summary>Complete AWG system configuration for graph construction.</summary>
public sealed record AwgSystemConfiguration
{
    public required string TopologyId { get; init; }

    public required string TopologyVersion { get; init; }

    public required AwgV3TopologyConfiguration Topology { get; init; }

    public required AwgAmbientBoundaryConfiguration Ambient { get; init; }

    public required AwgFanParameters Fan { get; init; }

    public required AwgSolarCollectorParameters SolarCollector { get; init; }

    public required SilicaGelParameters SilicaGel { get; init; }

    public required AwgCondenserParameters Condenser { get; init; }

    public required AwgPvParameters Pv { get; init; }

    public required BatteryParameters Battery { get; init; }

    public required IReadOnlyList<ElectricalLoadDemand> ElectricalLoads { get; init; }

    public double MpptEfficiencyFraction { get; init; } = 0.95;

    public AwgSystemConfiguration Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TopologyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(TopologyVersion);
        ArgumentNullException.ThrowIfNull(Topology);
        ArgumentNullException.ThrowIfNull(Ambient);
        ArgumentNullException.ThrowIfNull(Fan);
        ArgumentNullException.ThrowIfNull(SolarCollector);
        ArgumentNullException.ThrowIfNull(SilicaGel);
        ArgumentNullException.ThrowIfNull(Condenser);
        ArgumentNullException.ThrowIfNull(Pv);
        ArgumentNullException.ThrowIfNull(Battery);
        ArgumentNullException.ThrowIfNull(ElectricalLoads);

        Topology.Validate();
        Ambient.Validate();
        Fan.Validate();
        SolarCollector.Validate();
        SilicaGel.Validate();
        Condenser.Validate();
        Pv.Validate();
        Battery.Validate();
        foreach (var load in ElectricalLoads)
        {
            load.Validate();
        }

        if (Topology.EnableElectricalSubsystem && ElectricalLoads.Count == 0)
        {
            throw new ArgumentException(
                "Electrical subsystem requires at least one load demand.",
                nameof(ElectricalLoads));
        }

        if (!string.Equals(TopologyId, AwgV3TopologyIds.TopologyId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Unsupported topology id '{TopologyId}'. Expected '{AwgV3TopologyIds.TopologyId}'.",
                nameof(TopologyId));
        }

        return this;
    }
}

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
