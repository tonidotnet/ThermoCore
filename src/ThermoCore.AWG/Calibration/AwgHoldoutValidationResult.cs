using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>Fit-on-train / score-on-holdout calibration validation report (M5 workflow).</summary>
public sealed record AwgHoldoutValidationResult
{
    public required MeasurementDatasetSplit Split { get; init; }

    public required AwgParameterCalibrationResult Training { get; init; }

    public required MeasurementComparisonReport HoldoutBaselineReport { get; init; }

    public required MeasurementComparisonReport HoldoutFittedReport { get; init; }

    public required AwgSystemConfiguration FittedConfiguration { get; init; }

    public bool HoldoutImproved
        => !double.IsNaN(HoldoutFittedReport.OverallRmse)
           && !double.IsNaN(HoldoutBaselineReport.OverallRmse)
           && HoldoutFittedReport.OverallRmse <= HoldoutBaselineReport.OverallRmse + 1e-12;
}
