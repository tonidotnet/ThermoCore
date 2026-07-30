namespace ThermoCore.Core.Calibration;

/// <summary>Computes RMSE, MAE and bias between measured and simulated samples.</summary>
public static class ErrorMetricsCalculator
{
    public static ErrorMetrics Compute(IReadOnlyList<double> measured, IReadOnlyList<double> simulated)
    {
        ArgumentNullException.ThrowIfNull(measured);
        ArgumentNullException.ThrowIfNull(simulated);
        if (measured.Count == 0 || simulated.Count == 0)
        {
            throw new ArgumentException("Measured and simulated series must be non-empty.");
        }

        if (measured.Count != simulated.Count)
        {
            throw new ArgumentException("Measured and simulated series must have equal length.");
        }

        var n = measured.Count;
        var sumSq = 0.0;
        var sumAbs = 0.0;
        var sumSigned = 0.0;
        for (var i = 0; i < n; i++)
        {
            var residual = simulated[i] - measured[i];
            sumSq += residual * residual;
            sumAbs += Math.Abs(residual);
            sumSigned += residual;
        }

        return new ErrorMetrics
        {
            Rmse = Math.Sqrt(sumSq / n),
            Mae = sumAbs / n,
            Bias = sumSigned / n,
            SampleCount = n
        };
    }
}
