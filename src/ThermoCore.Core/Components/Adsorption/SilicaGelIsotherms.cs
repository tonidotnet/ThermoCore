using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

public sealed record SilicaGelIsothermContext
{
    public bool PreferDesorptionBranch { get; init; }
}

public sealed record SilicaGelIsothermMetadata
{
    public required string ModelName { get; init; }

    public required string Reference { get; init; }

    public required string ParameterSource { get; init; }

    public required string AdsorbentType { get; init; }

    public double MinimumTemperatureK { get; init; } = 250.0;

    public double MaximumTemperatureK { get; init; } = 400.0;

    public double MinimumRelativePressure { get; init; }

    public double MaximumRelativePressure { get; init; } = 1.0;
}

/// <summary>
/// Equilibrium isotherm contract (docs/03_Components/09_SilicaGel.md §13).
/// </summary>
public interface ISilicaGelIsotherm
{
    double CalculateEquilibriumLoadingKgPerKg(
        double bedTemperatureK,
        double vaporPressurePa,
        double saturationPressurePa,
        SilicaGelIsothermContext? context = null);

    SilicaGelIsothermMetadata Metadata { get; }
}

/// <summary>
/// Low-fidelity normalized polynomial isotherm:
/// X_eq = X_max * Σ a_i * r_p^i (docs/03_Components/09_SilicaGel.md §15).
/// </summary>
public sealed class GenericPolynomialIsotherm : ISilicaGelIsotherm
{
    private readonly double _maximumLoadingKgPerKg;
    private readonly IReadOnlyList<double> _coefficients;

    public GenericPolynomialIsotherm(
        double maximumLoadingKgPerKg,
        IReadOnlyList<double> polynomialCoefficientsAscendingPower,
        SilicaGelIsothermMetadata? metadata = null)
    {
        FiniteNumber.RequirePositive(maximumLoadingKgPerKg, nameof(maximumLoadingKgPerKg));
        ArgumentNullException.ThrowIfNull(polynomialCoefficientsAscendingPower);
        if (polynomialCoefficientsAscendingPower.Count == 0)
        {
            throw new ArgumentException("At least one polynomial coefficient is required.", nameof(polynomialCoefficientsAscendingPower));
        }

        foreach (var coefficient in polynomialCoefficientsAscendingPower)
        {
            FiniteNumber.Require(coefficient, nameof(polynomialCoefficientsAscendingPower));
        }

        _maximumLoadingKgPerKg = maximumLoadingKgPerKg;
        _coefficients = polynomialCoefficientsAscendingPower.ToArray();
        Metadata = metadata ?? new SilicaGelIsothermMetadata
        {
            ModelName = "GenericPolynomial",
            Reference = "docs/03_Components/09_SilicaGel.md §15",
            ParameterSource = "EngineeringEstimate",
            AdsorbentType = "SilicaGel"
        };
    }

    /// <summary>Linear isotherm X_eq = X_max * r_p for provisional engineering use.</summary>
    public static GenericPolynomialIsotherm CreateLinear(double maximumLoadingKgPerKg)
        => new(maximumLoadingKgPerKg, [1.0]);

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

        var relativePressure = Math.Clamp(vaporPressurePa / saturationPressurePa, 0.0, 1.0);
        var sum = 0.0;
        var power = relativePressure;
        for (var i = 0; i < _coefficients.Count; i++)
        {
            sum += _coefficients[i] * power;
            power *= relativePressure;
        }

        return Math.Clamp(sum * _maximumLoadingKgPerKg, 0.0, _maximumLoadingKgPerKg);
    }
}

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
