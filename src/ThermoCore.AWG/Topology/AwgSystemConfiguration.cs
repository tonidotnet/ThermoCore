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

    public required AwgWaterTankParameters WaterTank { get; init; }

    public AwgHeatRecoveryParameters HeatRecovery { get; init; } = new();

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
        ArgumentNullException.ThrowIfNull(WaterTank);
        ArgumentNullException.ThrowIfNull(Pv);
        ArgumentNullException.ThrowIfNull(Battery);
        ArgumentNullException.ThrowIfNull(ElectricalLoads);

        Topology.Validate();
        Ambient.Validate();
        Fan.Validate();
        SolarCollector.Validate();
        SilicaGel.Validate();
        Condenser.Validate();
        WaterTank.Validate();
        HeatRecovery.Validate();
        Pv.Validate();
        Battery.Validate();
        foreach (var load in ElectricalLoads)
        {
            load.Validate();
        }

        if (Topology.EnablePvRearAirChannel && !Topology.EnableElectricalSubsystem)
        {
            throw new ArgumentException(
                "PV rear-air channel requires the electrical subsystem.",
                nameof(Topology));
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
