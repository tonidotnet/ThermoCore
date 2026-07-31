namespace ThermoCore.App2.SolarAirHeater;

/// <summary>Grid sizing / feasibility report for the solar air heater MVP.</summary>
public sealed record SolarAirHeaterSizingResult
{
    public required IReadOnlyList<SolarAirHeaterSizingPointResult> Points { get; init; }

    public SolarAirHeaterSizingPointResult? BestUsefulHeat
        => Points.Where(p => p.Succeeded).OrderByDescending(p => p.UsefulHeatW).FirstOrDefault();

    public SolarAirHeaterSizingPointResult? BestTemperatureRise
        => Points.Where(p => p.Succeeded).OrderByDescending(p => p.TemperatureRiseK).FirstOrDefault();
}
