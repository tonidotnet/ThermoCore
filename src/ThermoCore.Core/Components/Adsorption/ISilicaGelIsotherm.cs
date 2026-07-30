using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

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
