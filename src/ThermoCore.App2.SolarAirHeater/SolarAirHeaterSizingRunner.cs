namespace ThermoCore.App2.SolarAirHeater;

/// <summary>Cartesian aperture × flow × irradiance sizing sweep (APP2-006).</summary>
public sealed class SolarAirHeaterSizingRunner
{
    private readonly SolarAirHeaterSimulationRunner _runner = new();

    public SolarAirHeaterSizingResult Run(
        SolarAirHeaterConfiguration baseline,
        IReadOnlyList<double> apertureAreasM2,
        IReadOnlyList<double> dryAirMassFlowsKgPerSecond,
        IReadOnlyList<double>? irradiancesWPerM2 = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(apertureAreasM2);
        ArgumentNullException.ThrowIfNull(dryAirMassFlowsKgPerSecond);
        if (apertureAreasM2.Count == 0 || dryAirMassFlowsKgPerSecond.Count == 0)
        {
            throw new ArgumentException("At least one aperture and one mass-flow value are required.");
        }

        var irradiances = irradiancesWPerM2 is { Count: > 0 }
            ? irradiancesWPerM2
            : [baseline.SolarIrradianceWPerM2];

        var points = new List<SolarAirHeaterSizingPointResult>(
            apertureAreasM2.Count * dryAirMassFlowsKgPerSecond.Count * irradiances.Count);

        foreach (var area in apertureAreasM2)
        {
            foreach (var flow in dryAirMassFlowsKgPerSecond)
            {
                foreach (var irradiance in irradiances)
                {
                    try
                    {
                        var configuration = baseline with
                        {
                            CollectorApertureAreaM2 = area,
                            DryAirMassFlowKgPerSecond = flow,
                            SolarIrradianceWPerM2 = irradiance
                        };
                        var run = _runner.Run(configuration);
                        points.Add(new SolarAirHeaterSizingPointResult
                        {
                            ApertureAreaM2 = area,
                            DryAirMassFlowKgPerSecond = flow,
                            SolarIrradianceWPerM2 = irradiance,
                            Succeeded = run.EngineResult.Succeeded,
                            TemperatureRiseK = run.TemperatureRiseK,
                            UsefulHeatW = run.UsefulHeatW,
                            SolarUtilizationFraction = run.SolarUtilizationFraction,
                            FailureMessage = run.EngineResult.Succeeded
                                ? null
                                : string.Join("; ", run.EngineResult.Diagnostics.Select(d => d.Code))
                        });
                    }
                    catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
                    {
                        points.Add(new SolarAirHeaterSizingPointResult
                        {
                            ApertureAreaM2 = area,
                            DryAirMassFlowKgPerSecond = flow,
                            SolarIrradianceWPerM2 = irradiance,
                            Succeeded = false,
                            TemperatureRiseK = 0,
                            UsefulHeatW = 0,
                            SolarUtilizationFraction = 0,
                            FailureMessage = ex.Message
                        });
                    }
                }
            }
        }

        return new SolarAirHeaterSizingResult { Points = points };
    }
}
