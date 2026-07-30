using ThermoCore.AWG.Simulation;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>Outcome of comparing an AWG run against imported measurements.</summary>
public sealed record AwgMeasurementValidationResult
{
    public required AwgSimulationRunResult Run { get; init; }

    public required MeasurementComparisonReport Report { get; init; }
}
