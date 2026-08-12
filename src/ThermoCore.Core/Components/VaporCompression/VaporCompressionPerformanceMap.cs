using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>
/// Provenance-aware vapor-compression performance-map contract (COOL-006 / COOL-007 / R5-001).
/// Map-based only — not a refrigerant cycle solver. Full plant wiring is R5-002.
/// </summary>
public sealed record VaporCompressionPerformanceMap
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string ProfileId { get; init; }

    public required string Manufacturer { get; init; }

    public required string Model { get; init; }

    public string HardwareClass { get; init; } = "vapor-compression-module";

    public required string SourceIdentifier { get; init; }

    public required string SourceRevision { get; init; }

    public required TecEvidenceLevel EvidenceLevel { get; init; }

    public required VaporCompressionValidityRange Validity { get; init; }

    public required IReadOnlyList<VaporCompressionMapPoint> MapPoints { get; init; }

    public VaporCompressionExtrapolationPolicy ExtrapolationPolicy { get; init; }
        = VaporCompressionExtrapolationPolicy.ClampWithDiagnostic;

    public VaporCompressionCyclingLimits Cycling { get; init; } = new();

    public VaporCompressionSafetyLimits Safety { get; init; } = new();

    /// <summary>Optional evaporator UA (W/K) for later plant models.</summary>
    public double? EvaporatorUaWPerK { get; init; }

    /// <summary>Optional condenser UA (W/K) for later plant models.</summary>
    public double? CondenserUaWPerK { get; init; }

    /// <summary>Optional dedicated condenser/evaporator fan electrical power (W).</summary>
    public double? FanElectricalPowerW { get; init; }

    public string? Refrigerant { get; init; }

    public string? FittingMethod { get; init; }

    public string? Notes { get; init; }

    public VaporCompressionPerformanceMap Validate()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Unsupported vapor-compression map schema '{SchemaVersion}'.",
                nameof(SchemaVersion));
        }

        RequireNonEmpty(ProfileId, nameof(ProfileId));
        RequireNonEmpty(Manufacturer, nameof(Manufacturer));
        RequireNonEmpty(Model, nameof(Model));
        RequireNonEmpty(HardwareClass, nameof(HardwareClass));
        RequireNonEmpty(SourceIdentifier, nameof(SourceIdentifier));
        RequireNonEmpty(SourceRevision, nameof(SourceRevision));

        if (!Enum.IsDefined(EvidenceLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(EvidenceLevel));
        }

        if (!Enum.IsDefined(ExtrapolationPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(ExtrapolationPolicy));
        }

        if (MapPoints.Count == 0)
        {
            throw new ArgumentException("MapPoints must not be empty.", nameof(MapPoints));
        }

        foreach (var point in MapPoints)
        {
            point.Validate();
        }

        Validity.Validate();
        Cycling.Validate();
        Safety.Validate();

        if (EvaporatorUaWPerK is { } evUa)
        {
            FiniteNumber.RequireNonNegative(evUa, nameof(EvaporatorUaWPerK));
        }

        if (CondenserUaWPerK is { } cdUa)
        {
            FiniteNumber.RequireNonNegative(cdUa, nameof(CondenserUaWPerK));
        }

        if (FanElectricalPowerW is { } fan)
        {
            FiniteNumber.RequireNonNegative(fan, nameof(FanElectricalPowerW));
        }

        return this;
    }

    private static void RequireNonEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }
    }
}
