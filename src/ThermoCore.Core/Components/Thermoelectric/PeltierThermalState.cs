using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// Dynamic face temperatures for TEC-005 thermal-mass mode.
/// </summary>
public sealed record PeltierThermalState
{
    public required double ColdFaceTemperatureK { get; init; }

    public required double HotFaceTemperatureK { get; init; }

    public static PeltierThermalState Create(double coldFaceTemperatureK, double hotFaceTemperatureK)
    {
        FiniteNumber.RequirePositive(coldFaceTemperatureK, nameof(coldFaceTemperatureK));
        FiniteNumber.RequirePositive(hotFaceTemperatureK, nameof(hotFaceTemperatureK));
        return new PeltierThermalState
        {
            ColdFaceTemperatureK = coldFaceTemperatureK,
            HotFaceTemperatureK = hotFaceTemperatureK
        };
    }
}
