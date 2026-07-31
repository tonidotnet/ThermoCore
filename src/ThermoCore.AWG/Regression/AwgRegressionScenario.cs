namespace ThermoCore.AWG.Regression;

/// <summary>Declarative AWG regression scenario (DOC-022 §17, APP-006).</summary>
public sealed record AwgRegressionScenario
{
    public required string Id { get; init; }

    public required string Description { get; init; }

    public double DurationSeconds { get; init; } = 30.0;

    public double TimeStepSeconds { get; init; } = 1.0;

    public bool EnableElectricalSubsystem { get; init; } = true;

    public bool EnableRecirculation { get; init; }

    public bool EnableHeatRecovery { get; init; }

    public bool EnablePvRearAirChannel { get; init; }

    public double AmbientTemperatureC { get; init; } = 25.0;

    public double RelativeHumidityFraction { get; init; } = 0.50;

    public double SolarIrradianceWPerSquareMeter { get; init; } = 800.0;

    public double InitialBatterySocFraction { get; init; } = 0.50;

    public double InitialWaterTankContentKg { get; init; }

    public double? WaterTankCapacityKg { get; init; }

    /// <summary>When set, overrides MVP silica-gel dry adsorbent mass (kg).</summary>
    public double? SilicaGelDryAdsorbentMassKg { get; init; }

    public bool RequireSuccess { get; init; } = true;

    public bool RequireBalancePass { get; init; } = true;
}
