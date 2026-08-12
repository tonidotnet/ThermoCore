using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Absorption;

/// <summary>
/// Research-only absorption performance-map contract (COOL-008 / DOC-037 / R7-001).
/// Not a detailed absorption-cycle solver; not wired into AWG production cooling selection.
/// </summary>
public sealed record AbsorptionPerformanceMap
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string ProfileId { get; init; }

    public required string Manufacturer { get; init; }

    public required string Model { get; init; }

    public string HardwareClass { get; init; } = "absorption-research-module";

    public required string SourceIdentifier { get; init; }

    public required string SourceRevision { get; init; }

    public required TecEvidenceLevel EvidenceLevel { get; init; }

    public required AbsorptionValidityRange Validity { get; init; }

    public required IReadOnlyList<AbsorptionMapPoint> MapPoints { get; init; }

    public AbsorptionExtrapolationPolicy ExtrapolationPolicy { get; init; }
        = AbsorptionExtrapolationPolicy.ClampWithDiagnostic;

    /// <summary>Working pair label (e.g. H2O/LiBr). Informational only.</summary>
    public string? WorkingPair { get; init; }

    public string? FittingMethod { get; init; }

    public string? Notes { get; init; }

    /// <summary>Always true for this milestone — absorption stays research-scoped.</summary>
    public bool ResearchOnly { get; init; } = true;

    public AbsorptionPerformanceMap Validate()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Unsupported absorption map schema '{SchemaVersion}'.",
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

        if (!ResearchOnly)
        {
            throw new ArgumentException(
                "Absorption maps must remain ResearchOnly=true under COOL-008.",
                nameof(ResearchOnly));
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
