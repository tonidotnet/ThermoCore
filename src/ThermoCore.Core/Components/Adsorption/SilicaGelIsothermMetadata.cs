using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

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
