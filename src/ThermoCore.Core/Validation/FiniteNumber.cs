namespace ThermoCore.Core.Validation;

/// <summary>
/// Rejects non-finite floating-point values at public calculation boundaries
/// (docs/07_ProjectManagement/18_CodingRules.md, docs/02_Mathematics/25_NumericalMethods.md).
/// </summary>
public static class FiniteNumber
{
    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    public static double Require(double value, string parameterName)
    {
        if (!IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Value must be a finite number (NaN and Infinity are rejected).");
        }

        return value;
    }

    public static double RequirePositive(double value, string parameterName)
    {
        Require(value, parameterName);
        if (value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Value must be finite and strictly positive.");
        }

        return value;
    }

    public static double RequireNonNegative(double value, string parameterName)
    {
        Require(value, parameterName);
        if (value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Value must be finite and non-negative.");
        }

        return value;
    }
}
