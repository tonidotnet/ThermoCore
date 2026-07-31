namespace ThermoCore.AWG.Regression;

/// <summary>Full report for an ambient T × RH matrix campaign.</summary>
public sealed record AwgAmbientMatrixReport
{
    public required IReadOnlyList<AwgAmbientMatrixPointResult> Points { get; init; }

    public AwgAmbientMatrixPointResult? BestLitersPerDay
        => Points.Where(p => p.Passed).OrderByDescending(p => p.LitersPerDay).FirstOrDefault();
}
