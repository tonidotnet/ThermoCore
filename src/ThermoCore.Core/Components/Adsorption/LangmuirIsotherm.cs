using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

/// <summary>
/// Langmuir-type isotherm X_eq = X_m * b(T) p_v / (1 + b(T) p_v)
/// with b(T) = b0 * exp(Q/(R T)) (docs/03_Components/09_SilicaGel.md §17).
/// </summary>
public sealed class LangmuirIsotherm : ISilicaGelIsotherm
{
    private readonly double _monolayerCapacityKgPerKg;
    private readonly double _affinityPreExponentialPerPa;
    private readonly double _affinityEnergyJPerMol;

    public LangmuirIsotherm(
        double monolayerCapacityKgPerKg,
        double affinityPreExponentialPerPa,
        double affinityEnergyJPerMol = 0.0,
        SilicaGelIsothermMetadata? metadata = null)
    {
        FiniteNumber.RequirePositive(monolayerCapacityKgPerKg, nameof(monolayerCapacityKgPerKg));
        FiniteNumber.RequirePositive(affinityPreExponentialPerPa, nameof(affinityPreExponentialPerPa));
        FiniteNumber.RequireNonNegative(affinityEnergyJPerMol, nameof(affinityEnergyJPerMol));

        _monolayerCapacityKgPerKg = monolayerCapacityKgPerKg;
        _affinityPreExponentialPerPa = affinityPreExponentialPerPa;
        _affinityEnergyJPerMol = affinityEnergyJPerMol;
        Metadata = metadata ?? new SilicaGelIsothermMetadata
        {
            ModelName = "Langmuir",
            Reference = "docs/03_Components/09_SilicaGel.md §17",
            ParameterSource = "EngineeringEstimate",
            AdsorbentType = "SilicaGel"
        };
    }

    public SilicaGelIsothermMetadata Metadata { get; }

    public double CalculateEquilibriumLoadingKgPerKg(
        double bedTemperatureK,
        double vaporPressurePa,
        double saturationPressurePa,
        SilicaGelIsothermContext? context = null)
    {
        FiniteNumber.RequirePositive(bedTemperatureK, nameof(bedTemperatureK));
        FiniteNumber.RequireNonNegative(vaporPressurePa, nameof(vaporPressurePa));
        FiniteNumber.RequirePositive(saturationPressurePa, nameof(saturationPressurePa));

        var affinity = _affinityPreExponentialPerPa;
        if (_affinityEnergyJPerMol > 0.0)
        {
            affinity *= Math.Exp(
                _affinityEnergyJPerMol
                / (Physics.PhysicalConstants.UniversalGasConstantJPerMolK * bedTemperatureK));
        }

        var bp = affinity * vaporPressurePa;
        var loading = _monolayerCapacityKgPerKg * bp / (1.0 + bp);
        return Math.Clamp(loading, 0.0, _monolayerCapacityKgPerKg);
    }
}
