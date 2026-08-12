namespace ThermoCore.Core.Calibration;

/// <summary>
/// Wide-format prototype CSV columns from
/// <c>docs/08_ResearchAndEvolution/40_PrototypeValidationPlan.md</c> (R3-001).
/// </summary>
public static class PrototypeWideCsvSchema
{
    public const string TimestampUtc = "timestampUtc";
    public const string TestId = "testId";
    public const string VariantId = "variantId";
    public const string AmbientTemperatureC = "ambientTemperatureC";
    public const string AmbientRhPercent = "ambientRhPercent";
    public const string InletTemperatureC = "inletTemperatureC";
    public const string InletRhPercent = "inletRhPercent";
    public const string OutletTemperatureC = "outletTemperatureC";
    public const string OutletRhPercent = "outletRhPercent";
    public const string ColdSurfaceTemperatureC = "coldSurfaceTemperatureC";
    public const string HotSideTemperatureC = "hotSideTemperatureC";
    public const string AirflowM3PerHour = "airflowM3PerHour";
    public const string VoltageV = "voltageV";
    public const string CurrentA = "currentA";
    public const string PowerW = "powerW";
    public const string SolarIrradianceWPerM2 = "solarIrradianceWPerM2";
    public const string WaterMassG = "waterMassG";
    public const string SorbentMassG = "sorbentMassG";
    public const string Notes = "notes";

    public static readonly string HeaderLine = string.Join(',',
    [
        TimestampUtc,
        TestId,
        VariantId,
        AmbientTemperatureC,
        AmbientRhPercent,
        InletTemperatureC,
        InletRhPercent,
        OutletTemperatureC,
        OutletRhPercent,
        ColdSurfaceTemperatureC,
        HotSideTemperatureC,
        AirflowM3PerHour,
        VoltageV,
        CurrentA,
        PowerW,
        SolarIrradianceWPerM2,
        WaterMassG,
        SorbentMassG,
        Notes
    ]);

    /// <summary>
    /// Default wide-column → long-format channel_id map for commercial Peltier baseline rows.
    /// Units follow the CSV column names (°C, %, W, …).
    /// </summary>
    public static IReadOnlyDictionary<string, (string ChannelId, string Unit)> DefaultChannelMap { get; }
        = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            [AmbientTemperatureC] = ("prototype.ambient.temperatureC", "C"),
            [AmbientRhPercent] = ("prototype.ambient.relativeHumidityPercent", "%"),
            [InletTemperatureC] = ("prototype.inlet.temperatureC", "C"),
            [InletRhPercent] = ("prototype.inlet.relativeHumidityPercent", "%"),
            [OutletTemperatureC] = ("prototype.outlet.temperatureC", "C"),
            [OutletRhPercent] = ("prototype.outlet.relativeHumidityPercent", "%"),
            [ColdSurfaceTemperatureC] = ("prototype.coldSurface.temperatureC", "C"),
            [HotSideTemperatureC] = ("prototype.hotSide.temperatureC", "C"),
            [AirflowM3PerHour] = ("prototype.airflow.m3PerHour", "m3/h"),
            [VoltageV] = ("prototype.electrical.voltageV", "V"),
            [CurrentA] = ("prototype.electrical.currentA", "A"),
            [PowerW] = ("prototype.electrical.powerW", "W"),
            [SolarIrradianceWPerM2] = ("prototype.solar.irradianceWPerM2", "W/m2"),
            [WaterMassG] = ("prototype.water.massG", "g"),
            [SorbentMassG] = ("prototype.sorbent.massG", "g")
        };
}

/// <summary>One wide-format prototype measurement row.</summary>
public sealed record PrototypeWideMeasurementRow
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public string? TestId { get; init; }

    public string? VariantId { get; init; }

    public double? AmbientTemperatureC { get; init; }

    public double? AmbientRhPercent { get; init; }

    public double? InletTemperatureC { get; init; }

    public double? InletRhPercent { get; init; }

    public double? OutletTemperatureC { get; init; }

    public double? OutletRhPercent { get; init; }

    public double? ColdSurfaceTemperatureC { get; init; }

    public double? HotSideTemperatureC { get; init; }

    public double? AirflowM3PerHour { get; init; }

    public double? VoltageV { get; init; }

    public double? CurrentA { get; init; }

    public double? PowerW { get; init; }

    public double? SolarIrradianceWPerM2 { get; init; }

    public double? WaterMassG { get; init; }

    public double? SorbentMassG { get; init; }

    public string? Notes { get; init; }
}
