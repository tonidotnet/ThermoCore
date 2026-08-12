using ThermoCore.Core.Calibration;

namespace ThermoCore.Core.Tests;

public class PrototypeMeasurementProfileTests
{
    [Fact]
    public void WideCsv_ImportsRequiredAndOptionalColumns()
    {
        var csv = """
            timestampUtc,testId,inletTemperatureC,inletRhPercent,powerW,waterMassG,airflowM3PerHour,notes
            2026-07-15T10:00:00Z,t1,28.0,55.0,40.0,10.0,45.0,ok
            2026-07-15T10:05:00Z,t1,28.1,54.0,41.0,20.0,,
            """;

        var (rows, warnings) = PrototypeWideCsvImporter.ImportRows(csv);
        Assert.Equal(2, rows.Count);
        Assert.Empty(warnings);
        Assert.Equal(28.0, rows[0].InletTemperatureC);
        Assert.Equal(45.0, rows[0].AirflowM3PerHour);
        Assert.Null(rows[1].AirflowM3PerHour);
        Assert.Equal(20.0, rows[1].WaterMassG);
    }

    [Fact]
    public void WideCsv_RejectsMissingCoreMeasurementColumns()
    {
        var csv = """
            timestampUtc,testId,notes
            2026-07-15T10:00:00Z,t1,no-measurements
            """;
        Assert.Throws<FormatException>(() => PrototypeWideCsvImporter.ImportRows(csv));
    }

    [Fact]
    public void CampaignDocument_RoundTripsAndPreservesHardwareSensorsAndValidationLevel()
    {
        var document = SampleCampaign(PrototypeValidationLevel.IntegratedValidated);
        var json = PrototypeCampaignDocumentLoader.SaveToJson(document);
        var loaded = PrototypeCampaignDocumentLoader.LoadFromJson(json);

        Assert.Equal(document.CampaignId, loaded.CampaignId);
        Assert.Equal(PrototypeValidationLevel.IntegratedValidated, loaded.ValidationLevel);
        Assert.Equal("Acme", loaded.Hardware.Manufacturer);
        Assert.Equal("PD-100", loaded.Hardware.Model);
        Assert.Equal("commercial-peltier-dehumidifier", loaded.Hardware.HardwareClass);
        Assert.Single(loaded.Sensors);
        Assert.Equal("PWR-1", loaded.Sensors[0].CalibrationId);
    }

    [Theory]
    [InlineData(PrototypeValidationLevel.BenchValidated)]
    [InlineData(PrototypeValidationLevel.IntegratedValidated)]
    [InlineData(PrototypeValidationLevel.OutdoorValidated)]
    public void ValidationLevel_IsDistinguished(PrototypeValidationLevel level)
    {
        var document = SampleCampaign(level);
        Assert.Equal(level, document.Validate().ValidationLevel);
    }

    [Fact]
    public void Invalid_EmptyHardwareManufacturer_IsRejected()
    {
        var document = SampleCampaign(PrototypeValidationLevel.BenchValidated) with
        {
            Hardware = SampleCampaign(PrototypeValidationLevel.BenchValidated).Hardware with
            {
                Manufacturer = " "
            }
        };
        Assert.ThrowsAny<ArgumentException>(() => document.Validate());
    }

    [Fact]
    public void Bridge_EmitsLongFormatDataset_WithCampaignMetadata()
    {
        var campaign = SampleCampaign(PrototypeValidationLevel.BenchValidated);
        var csv = """
            timestampUtc,inletTemperatureC,inletRhPercent,powerW,waterMassG,coldSurfaceTemperatureC
            2026-07-15T10:00:00Z,28.0,55.0,41.0,12.5,8.0
            """;
        var package = PrototypeWideCsvImporter.ImportPackage(campaign, csv, "inline-wide");
        var dataset = PrototypeMeasurementBridge.ToMeasurementDataset(package);

        Assert.Equal(campaign.CampaignId, dataset.Campaign!.CampaignId);
        Assert.Equal(PrototypeValidationLevel.BenchValidated, dataset.Campaign.ValidationLevel);
        Assert.Contains("prototype.inlet.temperatureC", dataset.ChannelIds);
        Assert.Contains("prototype.electrical.powerW", dataset.ChannelIds);
        Assert.Contains("prototype.water.massG", dataset.ChannelIds);
        Assert.Contains("prototype.coldSurface.temperatureC", dataset.ChannelIds);
        Assert.Equal(5, dataset.Samples.Count);
        Assert.All(dataset.Samples, s => Assert.False(string.IsNullOrWhiteSpace(s.Unit)));
    }

    [Fact]
    public void SampleCampaignFiles_LoadAndBridge()
    {
        var path = FindRepoFile(Path.Combine("samples", "calibration", "prototype-campaign.r3-001.json"));
        var package = PrototypeWideCsvImporter.ImportPackageFromFiles(path);
        Assert.Equal("r3-001-commercial-peltier-bench", package.Campaign.CampaignId);
        Assert.Equal(PrototypeValidationLevel.BenchValidated, package.Campaign.ValidationLevel);
        Assert.Equal(3, package.Rows.Count);
        Assert.Contains(package.Campaign.Sensors, s => s.Role == "power-meter");

        var dataset = PrototypeMeasurementBridge.ToMeasurementDataset(package);
        Assert.True(dataset.Samples.Count >= 3);
        Assert.Equal(package.Campaign.Hardware.Model, dataset.Campaign!.Hardware.Model);
    }

    private static PrototypeCampaignDocument SampleCampaign(PrototypeValidationLevel level)
        => new PrototypeCampaignDocument
        {
            CampaignId = "test-campaign",
            ValidationLevel = level,
            Hardware = new PrototypeHardwareIdentity
            {
                Manufacturer = "Acme",
                Model = "PD-100",
                HardwareClass = "commercial-peltier-dehumidifier",
                SerialNumber = "SN-1"
            },
            Sensors =
            [
                new PrototypeSensorCalibrationRef
                {
                    Role = "power-meter",
                    CalibrationId = "PWR-1",
                    Quantity = "electricalPower",
                    Unit = "W"
                }
            ],
            MeasurementCsvPath = "measurements.csv",
            SourceIdentifier = "thermocore://test",
            SourceRevision = "1"
        }.Validate();

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' from test base directory.");
    }
}
