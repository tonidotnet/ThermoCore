using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// Provenance-aware TEC hardware profile (COOL-003 / R2-001).
/// Maps to <see cref="AnalyticalPeltierParameters"/> without hard-coding a manufacturer in the physics model.
/// </summary>
public sealed record TecManufacturerProfile
{
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>Document schema version for forward-compatible serialization.</summary>
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public required string ProfileId { get; init; }

    public required string Manufacturer { get; init; }

    public required string Model { get; init; }

    public required TecParameterModelType ParameterModelType { get; init; }

    /// <summary>Stable source URI/path/id (datasheet, lab notebook, repo path).</summary>
    public required string SourceIdentifier { get; init; }

    /// <summary>Revision, date code, or document version of the source.</summary>
    public required string SourceRevision { get; init; }

    public required TecEvidenceLevel EvidenceLevel { get; init; }

    public double? LengthMm { get; init; }

    public double? WidthMm { get; init; }

    public double? HeightMm { get; init; }

    /// <summary>Datasheet Imax (A).</summary>
    public required double MaximumCurrentA { get; init; }

    /// <summary>Datasheet Vmax (V).</summary>
    public required double MaximumVoltageV { get; init; }

    /// <summary>Datasheet Qmax (W) at the hot-side reference.</summary>
    public required double MaximumCoolingPowerW { get; init; }

    /// <summary>Datasheet ΔTmax (K).</summary>
    public required double MaximumTemperatureDifferenceK { get; init; }

    /// <summary>Hot-side temperature used for datasheet ratings / estimation (K).</summary>
    public double HotSideReferenceTemperatureK { get; init; } = 300.0;

    public double MinimumColdSideTemperatureK { get; init; } = 250.0;

    public double MaximumHotSideTemperatureK { get; init; } = 360.0;

    public double MaximumElectricalPowerW { get; init; } = double.PositiveInfinity;

    /// <summary>
    /// Optional explicit α, R, K. When null and model type is analytical, coefficients
    /// are estimated from datasheet ratings (documented provisional method).
    /// </summary>
    public TecAnalyticalCoefficientSet? AnalyticalCoefficients { get; init; }

    /// <summary>Optional constant-COP value when <see cref="ParameterModelType"/> is ConstantCop.</summary>
    public double? ConstantCoolingCop { get; init; }

    public string? Notes { get; init; }

    /// <summary>Human-readable statement of fitting/estimation assumptions.</summary>
    public string? FittingMethod { get; init; }

    public TecManufacturerProfile Validate()
    {
        if (string.IsNullOrWhiteSpace(SchemaVersion))
        {
            throw new ArgumentException("SchemaVersion is required.", nameof(SchemaVersion));
        }

        if (!IsSupportedSchemaVersion(SchemaVersion))
        {
            throw new ArgumentException(
                $"Unsupported TEC profile schema version '{SchemaVersion}'. Supported: {CurrentSchemaVersion}.",
                nameof(SchemaVersion));
        }

        RequireNonEmpty(ProfileId, nameof(ProfileId));
        RequireNonEmpty(Manufacturer, nameof(Manufacturer));
        RequireNonEmpty(Model, nameof(Model));
        RequireNonEmpty(SourceIdentifier, nameof(SourceIdentifier));
        RequireNonEmpty(SourceRevision, nameof(SourceRevision));

        if (!Enum.IsDefined(ParameterModelType))
        {
            throw new ArgumentOutOfRangeException(nameof(ParameterModelType));
        }

        if (!Enum.IsDefined(EvidenceLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(EvidenceLevel));
        }

        FiniteNumber.RequirePositive(MaximumCurrentA, nameof(MaximumCurrentA));
        FiniteNumber.RequirePositive(MaximumVoltageV, nameof(MaximumVoltageV));
        FiniteNumber.RequirePositive(MaximumCoolingPowerW, nameof(MaximumCoolingPowerW));
        FiniteNumber.RequirePositive(MaximumTemperatureDifferenceK, nameof(MaximumTemperatureDifferenceK));
        FiniteNumber.RequirePositive(HotSideReferenceTemperatureK, nameof(HotSideReferenceTemperatureK));
        FiniteNumber.RequirePositive(MinimumColdSideTemperatureK, nameof(MinimumColdSideTemperatureK));
        FiniteNumber.RequirePositive(MaximumHotSideTemperatureK, nameof(MaximumHotSideTemperatureK));
        if (!double.IsPositiveInfinity(MaximumElectricalPowerW))
        {
            FiniteNumber.RequirePositive(MaximumElectricalPowerW, nameof(MaximumElectricalPowerW));
        }

        if (MinimumColdSideTemperatureK >= MaximumHotSideTemperatureK)
        {
            throw new ArgumentException(
                "Minimum cold-side temperature must be lower than maximum hot-side temperature.");
        }

        if (HotSideReferenceTemperatureK <= MaximumTemperatureDifferenceK)
        {
            throw new ArgumentException(
                "Hot-side reference temperature must exceed ΔTmax for datasheet estimation validity.");
        }

        if (LengthMm is { } length)
        {
            FiniteNumber.RequirePositive(length, nameof(LengthMm));
        }

        if (WidthMm is { } width)
        {
            FiniteNumber.RequirePositive(width, nameof(WidthMm));
        }

        if (HeightMm is { } height)
        {
            FiniteNumber.RequirePositive(height, nameof(HeightMm));
        }

        AnalyticalCoefficients?.Validate();

        switch (ParameterModelType)
        {
            case TecParameterModelType.AnalyticalSteadyState:
                break;
            case TecParameterModelType.ConstantCop:
                if (ConstantCoolingCop is not { } cop || cop <= 0.0 || !double.IsFinite(cop))
                {
                    throw new ArgumentException(
                        "ConstantCoolingCop must be a positive finite value for ConstantCop profiles.",
                        nameof(ConstantCoolingCop));
                }

                break;
            case TecParameterModelType.PerformanceMap:
                throw new ArgumentException(
                    "PerformanceMap profiles are not supported by schema 1.0 mapping yet.",
                    nameof(ParameterModelType));
            default:
                throw new ArgumentOutOfRangeException(nameof(ParameterModelType));
        }

        return this;
    }

    /// <summary>
    /// Builds analytical Peltier parameters for simulation.
    /// Existing callers of <see cref="AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults"/>
    /// remain valid; this is an additive path.
    /// </summary>
    public AnalyticalPeltierParameters ToAnalyticalPeltierParameters(
        AnalyticalPeltierParameters? thermalBoundaryDefaults = null)
    {
        Validate();
        if (ParameterModelType != TecParameterModelType.AnalyticalSteadyState)
        {
            throw new InvalidOperationException(
                $"Profile '{ProfileId}' uses {ParameterModelType}; analytical mapping requires AnalyticalSteadyState.");
        }

        var defaults = thermalBoundaryDefaults ?? AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults();
        var coefficients = AnalyticalCoefficients?.Validate() ?? EstimateAnalyticalCoefficientsFromDatasheet();

        var maxPower = double.IsPositiveInfinity(MaximumElectricalPowerW)
            ? MaximumVoltageV * MaximumCurrentA
            : MaximumElectricalPowerW;

        var activeArea = 0.0;
        if (LengthMm is { } length && WidthMm is { } width)
        {
            activeArea = (length * 1e-3) * (width * 1e-3);
        }

        return new AnalyticalPeltierParameters
        {
            SeebeckCoefficientVPerK = coefficients.SeebeckCoefficientVPerK,
            ElectricalResistanceOhm = coefficients.ElectricalResistanceOhm,
            ThermalConductanceWPerK = coefficients.ThermalConductanceWPerK,
            MaximumCurrentA = MaximumCurrentA,
            MaximumVoltageV = MaximumVoltageV,
            MaximumElectricalPowerW = maxPower,
            MaximumTemperatureDifferenceK = MaximumTemperatureDifferenceK,
            MaximumHotSideTemperatureK = MaximumHotSideTemperatureK,
            MinimumColdSideTemperatureK = MinimumColdSideTemperatureK,
            ColdSideThermalResistanceKPerW = defaults.ColdSideThermalResistanceKPerW,
            HotSideThermalResistanceKPerW = defaults.HotSideThermalResistanceKPerW,
            MinimumUsefulCoolingCop = defaults.MinimumUsefulCoolingCop,
            MaximumAllowedColdSideHeatFluxWPerM2 = defaults.MaximumAllowedColdSideHeatFluxWPerM2,
            ActiveColdSideAreaM2 = activeArea > 0.0 ? activeArea : defaults.ActiveColdSideAreaM2,
            EnableProtectionShutdown = defaults.EnableProtectionShutdown,
            HotSideThermalResistanceWarningKPerW = defaults.HotSideThermalResistanceWarningKPerW,
            ColdSideThermalResistanceWarningKPerW = defaults.ColdSideThermalResistanceWarningKPerW,
            EffectiveColdSideThermalCapacityJPerK = defaults.EffectiveColdSideThermalCapacityJPerK,
            EffectiveHotSideThermalCapacityJPerK = defaults.EffectiveHotSideThermalCapacityJPerK
        }.Validate();
    }

    /// <summary>
    /// Provisional datasheet → α,R,K estimation (docs/03_Components/08_Peltier.md §60 / Lineykin-style).
    /// Assumptions: ratings at hot-side reference; ΔT=0 informs V≈IR scaling via Vmax/Imax family.
    /// </summary>
    public TecAnalyticalCoefficientSet EstimateAnalyticalCoefficientsFromDatasheet()
    {
        Validate();
        var th = HotSideReferenceTemperatureK;
        var dT = MaximumTemperatureDifferenceK;
        var imax = MaximumCurrentA;
        var vmax = MaximumVoltageV;

        var alpha = vmax / th;
        var resistance = (th - dT) * vmax / (imax * th);
        var conductance = (th - dT) * vmax * imax / (2.0 * th * dT);

        return new TecAnalyticalCoefficientSet
        {
            SeebeckCoefficientVPerK = alpha,
            ElectricalResistanceOhm = resistance,
            ThermalConductanceWPerK = conductance
        }.Validate();
    }

    public static bool IsSupportedSchemaVersion(string schemaVersion)
        => string.Equals(schemaVersion, CurrentSchemaVersion, StringComparison.Ordinal);

    private static void RequireNonEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }
    }
}
