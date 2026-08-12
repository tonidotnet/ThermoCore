using System.Text.Json.Serialization;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Components.VaporCompression;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.AWG.Cooling;

/// <summary>
/// Optional AWG cooling-plant selection (COOL-002). Missing JSON → Thermoelectric default.
/// </summary>
public sealed record AwgCoolingPlantConfiguration
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CoolingTechnology Technology { get; init; } = CoolingTechnology.Thermoelectric;

    /// <summary>
    /// Optional path to an R3-001 prototype campaign JSON used to fit a commercial profile.
    /// Relative paths resolve against the process working directory unless absolute.
    /// </summary>
    public string? CommercialCampaignDocumentPath { get; init; }

    /// <summary>Inline commercial profile (tests / advanced callers). Takes precedence over path.</summary>
    [JsonIgnore]
    public CommercialPeltierDehumidifierProfile? CommercialProfile { get; init; }

    /// <summary>Optional path to a vapor-compression performance-map JSON (R5-001 schema).</summary>
    public string? VaporCompressionMapPath { get; init; }

    /// <summary>Inline VC map (tests / advanced callers). Takes precedence over path.</summary>
    [JsonIgnore]
    public VaporCompressionPerformanceMap? VaporCompressionMap { get; init; }

    public AwgCoolingPlantConfiguration Validate()
    {
        if (!Enum.IsDefined(Technology))
        {
            throw new ArgumentOutOfRangeException(nameof(Technology), Technology, "Unknown cooling technology.");
        }

        if (Technology == CoolingTechnology.AbsorptionResearch)
        {
            throw new ArgumentException(
                $"Cooling technology '{Technology}' is reserved for a later research milestone.",
                nameof(Technology));
        }

        if (Technology == CoolingTechnology.CommercialPeltierDehumidifier
            && CommercialProfile is null
            && string.IsNullOrWhiteSpace(CommercialCampaignDocumentPath))
        {
            throw new ArgumentException(
                "CommercialPeltierDehumidifier requires CommercialProfile or CommercialCampaignDocumentPath.",
                nameof(CommercialCampaignDocumentPath));
        }

        if (Technology == CoolingTechnology.VaporCompression
            && VaporCompressionMap is null
            && string.IsNullOrWhiteSpace(VaporCompressionMapPath))
        {
            throw new ArgumentException(
                "VaporCompression requires VaporCompressionMap or VaporCompressionMapPath.",
                nameof(VaporCompressionMapPath));
        }

        return this;
    }
}

/// <summary>Creates <see cref="ICoolingPlantModel"/> instances from AWG configuration.</summary>
public static class CoolingPlantFactory
{
    public static ICoolingPlantModel Create(
        AwgSystemConfiguration configuration,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        return Create(configuration.Cooling, configuration.Condenser, calculator);
    }

    public static ICoolingPlantModel Create(
        AwgCoolingPlantConfiguration cooling,
        AwgCondenserParameters condenserParameters,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentNullException.ThrowIfNull(cooling);
        ArgumentNullException.ThrowIfNull(condenserParameters);
        cooling.Validate();

        return cooling.Technology switch
        {
            CoolingTechnology.Thermoelectric =>
                new ThermoelectricCoolingPlantAdapter(condenserParameters, calculator),
            CoolingTechnology.CommercialPeltierDehumidifier =>
                new CommercialPeltierCoolingPlantAdapter(ResolveCommercialProfile(cooling), calculator),
            CoolingTechnology.VaporCompression =>
                new VaporCompressionCoolingPlantAdapter(
                    ResolveVaporCompressionMap(cooling),
                    condenserParameters,
                    calculator),
            _ => throw new ArgumentOutOfRangeException(
                nameof(cooling),
                cooling.Technology,
                "Unsupported cooling technology for factory create.")
        };
    }

    public static CommercialPeltierDehumidifierProfile ResolveCommercialProfile(
        AwgCoolingPlantConfiguration cooling)
    {
        ArgumentNullException.ThrowIfNull(cooling);
        if (cooling.CommercialProfile is { } inline)
        {
            return inline.Validate();
        }

        if (string.IsNullOrWhiteSpace(cooling.CommercialCampaignDocumentPath))
        {
            throw new ArgumentException(
                "Commercial campaign document path is required when no inline profile is supplied.",
                nameof(cooling));
        }

        var path = ResolvePath(cooling.CommercialCampaignDocumentPath);
        var package = PrototypeWideCsvImporter.ImportPackageFromFiles(path);
        return CommercialPeltierDehumidifierProfileFitter.FromPackage(package);
    }

    public static VaporCompressionPerformanceMap ResolveVaporCompressionMap(
        AwgCoolingPlantConfiguration cooling)
    {
        ArgumentNullException.ThrowIfNull(cooling);
        if (cooling.VaporCompressionMap is { } inline)
        {
            return inline.Validate();
        }

        if (string.IsNullOrWhiteSpace(cooling.VaporCompressionMapPath))
        {
            throw new ArgumentException(
                "Vapor-compression map path is required when no inline map is supplied.",
                nameof(cooling));
        }

        return VaporCompressionPerformanceMapSerializer.LoadFromFile(ResolvePath(cooling.VaporCompressionMapPath));
    }

    private static string ResolvePath(string path)
        => Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
}
