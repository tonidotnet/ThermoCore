using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>Fitted AWG parameters and before/after measurement comparison reports.</summary>
public sealed record AwgParameterCalibrationResult
{
    public required ParameterFittingResult Fitting { get; init; }

    public required AwgSystemConfiguration BaselineConfiguration { get; init; }

    public required AwgSystemConfiguration FittedConfiguration { get; init; }

    public required MeasurementComparisonReport BaselineReport { get; init; }

    public required MeasurementComparisonReport FittedReport { get; init; }
}
