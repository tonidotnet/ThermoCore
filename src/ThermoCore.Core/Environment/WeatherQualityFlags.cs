namespace ThermoCore.Core.Environment;

/// <summary>Quality and provenance flags for a weather sample (docs/04_Simulation/28_WeatherModel.md).</summary>
[Flags]
public enum WeatherQualityFlags
{
    None = 0,
    Measured = 1 << 0,
    Synthetic = 1 << 1,
    Interpolated = 1 << 2,
    PressureFallback = 1 << 3,
    GapFilled = 1 << 4,
    DerivedSolar = 1 << 5
}
