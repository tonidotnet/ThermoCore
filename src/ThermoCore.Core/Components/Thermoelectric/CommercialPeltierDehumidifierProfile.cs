using ThermoCore.Core.Calibration;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>One empirical operating point for a commercial Peltier dehumidifier map (COOL-005).</summary>
public sealed record CommercialPeltierMapPoint
{
    public required double InletTemperatureK { get; init; }

    public required double InletRelativeHumidityFraction { get; init; }

    public required double ElectricalPowerW { get; init; }

    public required double WaterProductionRateKgPerSecond { get; init; }

    public double? DryAirMassFlowKgPerSecond { get; init; }

    public double? OutletTemperatureK { get; init; }

    public double? OutletRelativeHumidityFraction { get; init; }

    public double? ColdSurfaceTemperatureK { get; init; }

    public double? HotSideTemperatureK { get; init; }

    public CommercialPeltierMapPoint Validate()
    {
        FiniteNumber.RequirePositive(InletTemperatureK, nameof(InletTemperatureK));
        FiniteNumber.Require(InletRelativeHumidityFraction, nameof(InletRelativeHumidityFraction));
        if (InletRelativeHumidityFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(InletRelativeHumidityFraction));
        }

        FiniteNumber.RequireNonNegative(ElectricalPowerW, nameof(ElectricalPowerW));
        FiniteNumber.RequireNonNegative(WaterProductionRateKgPerSecond, nameof(WaterProductionRateKgPerSecond));
        if (DryAirMassFlowKgPerSecond is { } flow)
        {
            FiniteNumber.RequireNonNegative(flow, nameof(DryAirMassFlowKgPerSecond));
        }

        if (OutletTemperatureK is { } tout)
        {
            FiniteNumber.RequirePositive(tout, nameof(OutletTemperatureK));
        }

        if (OutletRelativeHumidityFraction is { } rh)
        {
            FiniteNumber.Require(rh, nameof(OutletRelativeHumidityFraction));
            if (rh is < 0.0 or > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(OutletRelativeHumidityFraction));
            }
        }

        return this;
    }
}

/// <summary>Axis-aligned validity box — queries outside are clamped with a diagnostic.</summary>
public sealed record CommercialPeltierValidityRange
{
    public required double MinimumInletTemperatureK { get; init; }

    public required double MaximumInletTemperatureK { get; init; }

    public required double MinimumInletRelativeHumidityFraction { get; init; }

    public required double MaximumInletRelativeHumidityFraction { get; init; }

    public required double MinimumElectricalPowerW { get; init; }

    public required double MaximumElectricalPowerW { get; init; }

    public double? MinimumDryAirMassFlowKgPerSecond { get; init; }

    public double? MaximumDryAirMassFlowKgPerSecond { get; init; }

    public static CommercialPeltierValidityRange FromPoints(IReadOnlyList<CommercialPeltierMapPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            throw new ArgumentException("At least one map point is required.", nameof(points));
        }

        foreach (var point in points)
        {
            point.Validate();
        }

        var flows = points
            .Where(p => p.DryAirMassFlowKgPerSecond is not null)
            .Select(p => p.DryAirMassFlowKgPerSecond!.Value)
            .ToArray();

        return new CommercialPeltierValidityRange
        {
            // Expand AABB slightly so psychrometric round-trips and sensor noise near measured
            // points are not flagged as undocumented extrapolation.
            MinimumInletTemperatureK = points.Min(p => p.InletTemperatureK) - 0.5,
            MaximumInletTemperatureK = points.Max(p => p.InletTemperatureK) + 0.5,
            MinimumInletRelativeHumidityFraction = Math.Clamp(
                points.Min(p => p.InletRelativeHumidityFraction) - 0.02, 0.0, 1.0),
            MaximumInletRelativeHumidityFraction = Math.Clamp(
                points.Max(p => p.InletRelativeHumidityFraction) + 0.02, 0.0, 1.0),
            MinimumElectricalPowerW = Math.Max(0.0, points.Min(p => p.ElectricalPowerW) - 1.0),
            MaximumElectricalPowerW = points.Max(p => p.ElectricalPowerW) + 1.0,
            MinimumDryAirMassFlowKgPerSecond = flows.Length > 0 ? Math.Max(0.0, flows.Min() * 0.95) : null,
            MaximumDryAirMassFlowKgPerSecond = flows.Length > 0 ? flows.Max() * 1.05 : null
        }.Validate();
    }

    public CommercialPeltierValidityRange Validate()
    {
        FiniteNumber.RequirePositive(MinimumInletTemperatureK, nameof(MinimumInletTemperatureK));
        FiniteNumber.RequirePositive(MaximumInletTemperatureK, nameof(MaximumInletTemperatureK));
        if (MinimumInletTemperatureK > MaximumInletTemperatureK)
        {
            throw new ArgumentException("Inlet temperature validity range is inverted.");
        }

        FiniteNumber.Require(MinimumInletRelativeHumidityFraction, nameof(MinimumInletRelativeHumidityFraction));
        FiniteNumber.Require(MaximumInletRelativeHumidityFraction, nameof(MaximumInletRelativeHumidityFraction));
        if (MinimumInletRelativeHumidityFraction > MaximumInletRelativeHumidityFraction)
        {
            throw new ArgumentException("Inlet RH validity range is inverted.");
        }

        FiniteNumber.RequireNonNegative(MinimumElectricalPowerW, nameof(MinimumElectricalPowerW));
        FiniteNumber.RequireNonNegative(MaximumElectricalPowerW, nameof(MaximumElectricalPowerW));
        if (MinimumElectricalPowerW > MaximumElectricalPowerW)
        {
            throw new ArgumentException("Electrical power validity range is inverted.");
        }

        return this;
    }
}

/// <summary>Provenance-aware commercial dehumidifier black-box profile (COOL-005 / R3-002).</summary>
public sealed record CommercialPeltierDehumidifierProfile
{
    public const string CurrentSchemaVersion = "1.0";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string ProfileId { get; init; }

    public required string Manufacturer { get; init; }

    public required string Model { get; init; }

    public string HardwareClass { get; init; } = "commercial-peltier-dehumidifier";

    public required string SourceIdentifier { get; init; }

    public required string SourceRevision { get; init; }

    public required TecEvidenceLevel EvidenceLevel { get; init; }

    public PrototypeValidationLevel? ValidationLevel { get; init; }

    public string? CampaignId { get; init; }

    public required CommercialPeltierValidityRange Validity { get; init; }

    public required IReadOnlyList<CommercialPeltierMapPoint> MapPoints { get; init; }

    public bool SupportsOutletState { get; init; }

    public bool SupportsAirflowAxis { get; init; }

    public string? FittingMethod { get; init; }

    public string? Notes { get; init; }

    public CommercialPeltierDehumidifierProfile Validate()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Unsupported commercial Peltier profile schema '{SchemaVersion}'.",
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
