using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>Optional path switches for the AWG V3 topology (docs/04_Simulation/15_SystemTopology.md §12).</summary>
public sealed record AwgV3TopologyConfiguration
{
    public required bool EnableRecirculation { get; init; }

    public required bool EnableHeatRecovery { get; init; }

    public required bool EnablePvRearAirChannel { get; init; }

    public required bool EnableElectricalSubsystem { get; init; }

    public required double InitialRecirculationFraction { get; init; }

    public required string HeatRecoveryColdSideSource { get; init; }

    public required IReadOnlyDictionary<string, string> ComponentModelSelections { get; init; }

    public AwgV3TopologyConfiguration Validate()
    {
        FiniteNumber.Require(InitialRecirculationFraction, nameof(InitialRecirculationFraction));
        if (InitialRecirculationFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InitialRecirculationFraction),
                "Recirculation fraction must be in [0, 1].");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(HeatRecoveryColdSideSource);
        ArgumentNullException.ThrowIfNull(ComponentModelSelections);

        if (EnableRecirculation && InitialRecirculationFraction <= 0.0)
        {
            throw new ArgumentException(
                "Recirculation is enabled but InitialRecirculationFraction is zero.",
                nameof(InitialRecirculationFraction));
        }

        if (!EnableRecirculation && InitialRecirculationFraction > 0.0)
        {
            throw new ArgumentException(
                "Recirculation fraction must be zero when recirculation is disabled.",
                nameof(InitialRecirculationFraction));
        }

        return this;
    }
}
