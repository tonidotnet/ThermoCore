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
}
