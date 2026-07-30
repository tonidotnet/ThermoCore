using ThermoCore.Core.Components;
using ThermoCore.Core.Environment;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Results;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class WeatherAndCsvExportTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void ConstantWeatherProvider_ReturnsConfiguredState()
    {
        var provider = ConstantWeatherProvider.FromAmbient(
            UnitConversions.CelsiusToKelvin(30.0),
            0.40,
            irradianceWPerM2: 700.0);

        var state = provider.GetState(DateTimeOffset.Parse("2026-07-01T12:00:00Z"));
        Assert.Equal(UnitConversions.CelsiusToKelvin(30.0), state.AmbientTemperatureK);
        Assert.Equal(0.40, state.RelativeHumidityFraction);
        Assert.Equal(700.0, state.GlobalHorizontalIrradianceWPerM2);
        Assert.Equal(WeatherQualityFlags.Synthetic, state.QualityFlags);
    }

    [Fact]
    public void InterpolatingWeatherProvider_LinearlyInterpolatesTemperatureAndIrradiance()
    {
        var t0 = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var t1 = DateTimeOffset.Parse("2026-07-01T02:00:00Z");
        var series = new WeatherTimeSeries
        {
            Metadata = new WeatherSourceMetadata
            {
                SourceName = "test",
                SourceVersion = "1.0",
                LocationName = "lab",
                LatitudeDegrees = 0,
                LongitudeDegrees = 0,
                ElevationM = 0,
                TimezoneId = "UTC",
                DataLicense = "test",
                DerivedFields = Array.Empty<string>()
            },
            States =
            [
                new WeatherState
                {
                    TimestampUtc = t0,
                    AmbientTemperatureK = 300.0,
                    RelativeHumidityFraction = 0.4,
                    AbsolutePressurePa = PhysicalConstants.StandardAtmosphericPressurePa,
                    WindSpeedMPerSecond = 0.0,
                    GlobalHorizontalIrradianceWPerM2 = 0.0,
                    QualityFlags = WeatherQualityFlags.Measured
                },
                new WeatherState
                {
                    TimestampUtc = t1,
                    AmbientTemperatureK = 310.0,
                    RelativeHumidityFraction = 0.6,
                    AbsolutePressurePa = PhysicalConstants.StandardAtmosphericPressurePa,
                    WindSpeedMPerSecond = 2.0,
                    GlobalHorizontalIrradianceWPerM2 = 800.0,
                    QualityFlags = WeatherQualityFlags.Measured
                }
            ]
        }.Validate();

        var mid = new InterpolatingWeatherProvider(series).GetState(DateTimeOffset.Parse("2026-07-01T01:00:00Z"));
        Assert.Equal(305.0, mid.AmbientTemperatureK, precision: 10);
        Assert.Equal(0.5, mid.RelativeHumidityFraction, precision: 10);
        Assert.Equal(400.0, mid.GlobalHorizontalIrradianceWPerM2, precision: 10);
        Assert.True(mid.QualityFlags.HasFlag(WeatherQualityFlags.Interpolated));
    }

    [Fact]
    public void SyntheticDiurnalWeatherProvider_PeaksIrradianceAtMidday()
    {
        var start = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var provider = SyntheticDiurnalWeatherProvider.CreateDefault(start);
        var night = provider.GetState(start.AddHours(2));
        var noon = provider.GetState(start.AddHours(12));
        Assert.Equal(0.0, night.GlobalHorizontalIrradianceWPerM2);
        Assert.True(noon.GlobalHorizontalIrradianceWPerM2 > 800.0);
    }

    [Fact]
    public void WeatherDrivenAmbientSource_FollowsProviderTemperature()
    {
        var provider = ConstantWeatherProvider.FromAmbient(
            UnitConversions.CelsiusToKelvin(35.0),
            0.55,
            500.0);
        var source = new WeatherDrivenAmbientAirSourceComponent("ambient", provider, 0.02, _calculator);
        var context = new ComponentStepContext
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
        };

        var result = source.Evaluate(context);
        var outlet = Assert.IsType<MoistAirState>(result.OutputStates["outlet"]);
        Assert.Equal(UnitConversions.CelsiusToKelvin(35.0), outlet.TemperatureK, precision: 8);
    }

    [Fact]
    public void CsvExporter_WritesWideLongAndSummary()
    {
        var inlet = _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            0.5,
            0.02);
        var request = new SimulationRequest
        {
            Graph = new SimulationGraph(
                [
                    new AmbientAirSourceComponent("air", inlet),
                    new SensibleHeaterComponent("heater", 150.0, _calculator),
                    new ExhaustAirSinkComponent("sink")
                ],
                [
                    new PhysicalConnection
                    {
                        Id = "a_h",
                        SourceComponentId = "air",
                        SourcePortId = "outlet",
                        TargetComponentId = "heater",
                        TargetPortId = "inlet"
                    },
                    new PhysicalConnection
                    {
                        Id = "h_s",
                        SourceComponentId = "heater",
                        SourcePortId = "outlet",
                        TargetComponentId = "sink",
                        TargetPortId = "inlet"
                    }
                ]),
            StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            Duration = TimeSpan.FromSeconds(2),
            TimeStep = TimeSpan.FromSeconds(1)
        };

        var run = new SimulationEngine().Run(request);
        Assert.True(run.Succeeded);
        var result = SimulationResultCollector.Collect(run, request);

        var directory = Path.Combine(Path.GetTempPath(), "thermocore-csv-" + Guid.NewGuid().ToString("N"));
        try
        {
            SimulationResultCsvExporter.ExportDirectory(result, directory, run);
            Assert.True(File.Exists(Path.Combine(directory, "summary.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "series-wide.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "series-long.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "diagnostics.csv")));
            Assert.True(File.Exists(Path.Combine(directory, "balances.csv")));

            var wide = File.ReadAllText(Path.Combine(directory, "series-wide.csv"));
            Assert.Contains("timestamp_utc", wide, StringComparison.Ordinal);
            Assert.Contains("heater_outlet_temperature", wide, StringComparison.Ordinal);
            var longCsv = File.ReadAllText(Path.Combine(directory, "series-long.csv"));
            Assert.Contains("channel_id", longCsv, StringComparison.Ordinal);
            Assert.Contains("heater.outlet.temperature", longCsv, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
