namespace ThermoCore.Core.Psychrometrics;

public interface ISaturationPressureProvider
{
    double CalculatePressurePa(double temperatureK);

    SaturationPressureModelInfo ModelInfo { get; }
}
