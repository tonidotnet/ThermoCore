namespace ThermoCore.Core.Calibration;

/// <summary>Scalar comparison metrics for one channel (CAL-005).</summary>
public sealed record ErrorMetrics
{
    public required double Rmse { get; init; }

    public required double Mae { get; init; }

    public required double Bias { get; init; }

    public required int SampleCount { get; init; }
}
