using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

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
