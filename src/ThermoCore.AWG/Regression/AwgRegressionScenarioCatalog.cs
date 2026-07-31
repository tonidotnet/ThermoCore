using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThermoCore.AWG.Regression;

/// <summary>Built-in DOC-022 regression scenarios and JSON loaders.</summary>
public static class AwgRegressionScenarioCatalog
{
    public static JsonSerializerOptions CreateSerializerOptions()
        => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    public static IReadOnlyList<AwgRegressionScenario> CreateDefaultScenarios()
        =>
        [
            new AwgRegressionScenario
            {
                Id = "no-recirculation",
                Description = "Baseline open airflow path without recirculation."
            },
            new AwgRegressionScenario
            {
                Id = "recirculation-20",
                Description = "20% recirculation with cyclic tear solver.",
                EnableRecirculation = true,
                EnableElectricalSubsystem = false
            },
            new AwgRegressionScenario
            {
                Id = "heat-recovery",
                Description = "Sensible heat recovery on exhaust/inlet with torn loop.",
                EnableHeatRecovery = true,
                EnableElectricalSubsystem = false
            },
            new AwgRegressionScenario
            {
                Id = "heat-recovery-recirculation",
                Description = "Combined heat recovery and recirculation with two torn loops.",
                EnableHeatRecovery = true,
                EnableRecirculation = true,
                EnableElectricalSubsystem = false,
                DurationSeconds = 20
            },
            new AwgRegressionScenario
            {
                Id = "warm-humid-day",
                Description = "Warm humid ambient with electrical subsystem.",
                AmbientTemperatureC = 32.0,
                RelativeHumidityFraction = 0.75,
                SolarIrradianceWPerSquareMeter = 700.0
            },
            new AwgRegressionScenario
            {
                Id = "hot-dry-day",
                Description = "Hot dry high-solar regeneration case.",
                AmbientTemperatureC = 40.0,
                RelativeHumidityFraction = 0.20,
                SolarIrradianceWPerSquareMeter = 1000.0
            },
            new AwgRegressionScenario
            {
                Id = "dry-cool-day",
                Description = "Cool dry moderate-solar case for scenario-pack coverage.",
                AmbientTemperatureC = 12.0,
                RelativeHumidityFraction = 0.25,
                SolarIrradianceWPerSquareMeter = 550.0
            },
            new AwgRegressionScenario
            {
                Id = "low-battery",
                Description = "Low initial battery state of charge.",
                InitialBatterySocFraction = 0.12,
                DurationSeconds = 20
            },
            new AwgRegressionScenario
            {
                Id = "tank-near-full",
                Description = "Water tank starts near capacity.",
                WaterTankCapacityKg = 1.0,
                InitialWaterTankContentKg = 0.95,
                DurationSeconds = 15
            },
            new AwgRegressionScenario
            {
                Id = "pv-rear-air",
                Description = "Process air through PV rear channel before collector.",
                EnablePvRearAirChannel = true,
                DurationSeconds = 20
            }
        ];

    public static AwgRegressionScenario LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<AwgRegressionScenario>(json, CreateSerializerOptions())
            ?? throw new InvalidOperationException("Scenario JSON deserialized to null.");
    }

    public static AwgRegressionScenario LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return LoadFromJson(File.ReadAllText(path));
    }

    public static IReadOnlyList<AwgRegressionScenario> LoadFromDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Scenario directory was not found: {directory}");
        }

        return Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToArray();
    }

    /// <summary>
    /// Dry air (30% RH) with strong sunshine, ample battery, temperature × silica-gel mass matrix.
    /// </summary>
    public static IReadOnlyList<AwgRegressionScenario> CreateDrySunnyMatrixScenarios()
    {
        double[] temperaturesC = [10, 15, 20, 25, 30, 35];
        double[] silicaKg = [1, 2, 3, 4, 5];
        var scenarios = new List<AwgRegressionScenario>(temperaturesC.Length * silicaKg.Length);
        foreach (var temperatureC in temperaturesC)
        {
            foreach (var massKg in silicaKg)
            {
                var tempLabel = temperatureC.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                var massLabel = massKg.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                scenarios.Add(new AwgRegressionScenario
                {
                    Id = $"dry-sunny-T{tempLabel}C-silica{massLabel}kg",
                    Description =
                        $"Dry sunny day: {tempLabel} °C, 30% RH, G=950 W/m², " +
                        $"battery SOC 90%, silica gel {massLabel} kg dry adsorbent.",
                    DurationSeconds = 30,
                    TimeStepSeconds = 1,
                    EnableElectricalSubsystem = true,
                    AmbientTemperatureC = temperatureC,
                    RelativeHumidityFraction = 0.30,
                    SolarIrradianceWPerSquareMeter = 950.0,
                    InitialBatterySocFraction = 0.90,
                    SilicaGelDryAdsorbentMassKg = massKg,
                    RequireSuccess = true,
                    RequireBalancePass = true
                });
            }
        }

        return scenarios;
    }

    /// <summary>
    /// Full AWG V3 process train (heat recovery + electrical), same dry-sunny air/solar boundaries
    /// and 0.02 kg/s process flow as the dry-sunny matrix.
    /// </summary>
    public static IReadOnlyList<AwgRegressionScenario> CreateFullAwgFlowDrySunnyScenarios()
    {
        double[] temperaturesC = [10, 15, 20, 25, 30, 35];
        double[] silicaKg = [1, 2, 3, 4, 5];
        var scenarios = new List<AwgRegressionScenario>(temperaturesC.Length * silicaKg.Length);
        foreach (var temperatureC in temperaturesC)
        {
            foreach (var massKg in silicaKg)
            {
                var tempLabel = temperatureC.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                var massLabel = massKg.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                scenarios.Add(new AwgRegressionScenario
                {
                    Id = $"full-awg-T{tempLabel}C-silica{massLabel}kg",
                    Description =
                        $"Full AWG flow (HR+electrical): {tempLabel} °C, 30% RH, G=950 W/m², " +
                        $"battery SOC 90%, silica {massLabel} kg, ṁ=0.02 kg/s.",
                    DurationSeconds = 30,
                    TimeStepSeconds = 1,
                    EnableElectricalSubsystem = true,
                    EnableHeatRecovery = true,
                    AmbientTemperatureC = temperatureC,
                    RelativeHumidityFraction = 0.30,
                    SolarIrradianceWPerSquareMeter = 950.0,
                    InitialBatterySocFraction = 0.90,
                    SilicaGelDryAdsorbentMassKg = massKg,
                    RequireSuccess = true,
                    RequireBalancePass = true
                });
            }
        }

        return scenarios;
    }

    /// <summary>Canonical single full-flow demo case used for station diagrams.</summary>
    public static AwgRegressionScenario CreateFullAwgFlowDemoScenario()
        => new()
        {
            Id = "full-awg-flow-demo",
            Description =
                "Full AWG V3 path with HR: dry sunny 25 °C / 30% RH / G=950, " +
                "silica 2 kg, battery 90%, process air 0.02 kg/s.",
            DurationSeconds = 120,
            TimeStepSeconds = 1,
            EnableElectricalSubsystem = true,
            EnableHeatRecovery = true,
            AmbientTemperatureC = 25.0,
            RelativeHumidityFraction = 0.30,
            SolarIrradianceWPerSquareMeter = 950.0,
            InitialBatterySocFraction = 0.90,
            SilicaGelDryAdsorbentMassKg = 2.0,
            RequireSuccess = true,
            RequireBalancePass = true
        };

    /// <summary>
    /// Full AWG ambient matrix: inlet T ∈ {20,25,30,35} °C × RH ∈ {30,35,40,45,50,60}% 
    /// with fixed silica 2 kg and process air 0.02 kg/s.
    /// </summary>
    public static IReadOnlyList<AwgRegressionScenario> CreateFullAwgFlowAmbientMatrixScenarios()
    {
        double[] temperaturesC = [20, 25, 30, 35];
        double[] relativeHumidityPercents = [30, 35, 40, 45, 50, 60];
        var scenarios = new List<AwgRegressionScenario>(temperaturesC.Length * relativeHumidityPercents.Length);
        foreach (var temperatureC in temperaturesC)
        {
            foreach (var rhPercent in relativeHumidityPercents)
            {
                var tempLabel = temperatureC.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                var rhLabel = rhPercent.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                scenarios.Add(new AwgRegressionScenario
                {
                    Id = $"full-awg-T{tempLabel}C-RH{rhLabel}",
                    Description =
                        $"Full AWG ambient matrix (controlled): {tempLabel} °C, RH {rhLabel}%, G=950 W/m², " +
                        "silica 2 kg, regenerated start, battery SOC 90%, ṁ=0.02 kg/s, electrical (no HR).",
                    // Long enough for ≥1 adsorb→regen cycle with 2 min dwell.
                    DurationSeconds = 7200,
                    TimeStepSeconds = 5,
                    EnableElectricalSubsystem = true,
                    EnableHeatRecovery = false,
                    EnableController = true,
                    AmbientTemperatureC = temperatureC,
                    RelativeHumidityFraction = rhPercent / 100.0,
                    SolarIrradianceWPerSquareMeter = 950.0,
                    InitialBatterySocFraction = 0.90,
                    SilicaGelDryAdsorbentMassKg = 2.0,
                    InitialSilicaGelLoadingKgPerKg = 0.02,
                    RequireSuccess = true,
                    RequireBalancePass = true
                });
            }
        }

        return scenarios;
    }

    /// <summary>
    /// Controlled Full AWG at fixed 35 °C / 50% RH, sweeping silica-gel dry mass.
    /// </summary>
    public static IReadOnlyList<AwgRegressionScenario> CreateFullAwgFlowSilicaMassMatrixScenarios()
    {
        double[] silicaKg = [1, 2, 3, 4, 5];
        var scenarios = new List<AwgRegressionScenario>(silicaKg.Length);
        foreach (var massKg in silicaKg)
        {
            var massLabel = massKg.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            scenarios.Add(CreateControlledBaseline35C50RhScenario(
                id: $"full-awg-T35C-RH50-silica{massLabel}kg",
                description:
                    $"Controlled Full AWG silica sweep: 35 °C, RH 50%, G=950 W/m², " +
                    $"silica {massLabel} kg, Peltier 120 W, regenerated start, 2 h.",
                silicaKg: massKg,
                peltierW: 120.0));
        }

        return scenarios;
    }

    /// <summary>
    /// Controlled Full AWG at fixed 35 °C / 50% RH, sweeping nominal Peltier power.
    /// </summary>
    public static IReadOnlyList<AwgRegressionScenario> CreateFullAwgFlowPeltierPowerMatrixScenarios()
    {
        double[] peltierW = [40, 80, 120, 160, 200];
        var scenarios = new List<AwgRegressionScenario>(peltierW.Length);
        foreach (var powerW in peltierW)
        {
            var powerLabel = powerW.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            scenarios.Add(CreateControlledBaseline35C50RhScenario(
                id: $"full-awg-T35C-RH50-peltier{powerLabel}W",
                description:
                    $"Controlled Full AWG Peltier sweep: 35 °C, RH 50%, G=950 W/m², " +
                    $"silica 2 kg, Peltier {powerLabel} W, regenerated start, 2 h.",
                silicaKg: 2.0,
                peltierW: powerW));
        }

        return scenarios;
    }

    private static AwgRegressionScenario CreateControlledBaseline35C50RhScenario(
        string id,
        string description,
        double silicaKg,
        double peltierW)
        => new()
        {
            Id = id,
            Description = description,
            DurationSeconds = 7200,
            TimeStepSeconds = 5,
            EnableElectricalSubsystem = true,
            EnableHeatRecovery = false,
            EnableController = true,
            AmbientTemperatureC = 35.0,
            RelativeHumidityFraction = 0.50,
            SolarIrradianceWPerSquareMeter = 950.0,
            InitialBatterySocFraction = 0.90,
            SilicaGelDryAdsorbentMassKg = silicaKg,
            InitialSilicaGelLoadingKgPerKg = 0.02,
            NominalPeltierPowerRequestW = peltierW,
            RequireSuccess = true,
            RequireBalancePass = true
        };

    public static void WriteDefaultScenarios(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        var options = CreateSerializerOptions();
        foreach (var scenario in CreateDefaultScenarios())
        {
            var path = Path.Combine(directory, scenario.Id + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(scenario, options));
        }
    }

    public static void WriteScenarios(string directory, IEnumerable<AwgRegressionScenario> scenarios)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(scenarios);
        Directory.CreateDirectory(directory);
        var options = CreateSerializerOptions();
        foreach (var scenario in scenarios)
        {
            var path = Path.Combine(directory, scenario.Id + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(scenario, options));
        }
    }
}
