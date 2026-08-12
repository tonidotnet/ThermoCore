using ThermoCore.Core.Calibration;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class CommercialPeltierBlackBoxTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void SampleCampaign_FitsProfileWithProvenanceAndValidity()
    {
        var package = LoadSamplePackage();
        var profile = CommercialPeltierDehumidifierProfileFitter.FromPackage(package);

        Assert.Equal("commercial-peltier:r3-001-commercial-peltier-bench", profile.ProfileId);
        Assert.Equal("Generic", profile.Manufacturer);
        Assert.Equal("CommercialPeltierDehumidifier-Demo", profile.Model);
        Assert.Equal(TecEvidenceLevel.MeasuredPrototype, profile.EvidenceLevel);
        Assert.Equal(PrototypeValidationLevel.BenchValidated, profile.ValidationLevel);
        Assert.Equal("r3-001-commercial-peltier-bench", profile.CampaignId);
        Assert.Equal("thermocore://samples/calibration/r3-001", profile.SourceIdentifier);
        Assert.Equal(2, profile.MapPoints.Count);
        Assert.True(profile.SupportsOutletState);
        Assert.False(profile.SupportsAirflowAxis);

        // 12.5 g over 300 s → 4.166…e-5 kg/s
        Assert.Equal(12.5e-3 / 300.0, profile.MapPoints[0].WaterProductionRateKgPerSecond, precision: 12);
        Assert.Equal(12.5e-3 / 300.0, profile.MapPoints[1].WaterProductionRateKgPerSecond, precision: 12);
    }

    [Fact]
    public void Model_AtMapClimate_ProducesWaterAndOutlet()
    {
        var profile = CommercialPeltierDehumidifierProfileFitter.FromPackage(LoadSamplePackage());
        var model = new CommercialPeltierDehumidifierModel(profile, _calculator);
        var point = profile.MapPoints[0];
        var inlet = _calculator.CreateFromRelativeHumidity(
            point.InletTemperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            point.InletRelativeHumidityFraction,
            0.015);
        var result = model.Evaluate(inlet, electricalPowerOverrideW: point.ElectricalPowerW);

        Assert.True(result.WaterProductionRateKgPerSecond > 0.0);
        Assert.Equal(point.ElectricalPowerW, result.ElectricalPowerW, precision: 6);
        Assert.True(result.OutletStateFromMap);
        Assert.True(result.Outlet.TemperatureK < inlet.TemperatureK);
        Assert.False(result.OutsideValidity);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == "COMMERCIAL_PELTIER.OUTSIDE_VALIDITY");
    }

    [Fact]
    public void Model_OutsideValidity_ClampsWithoutExtrapolation()
    {
        var profile = CommercialPeltierDehumidifierProfileFitter.FromPackage(LoadSamplePackage());
        var model = new CommercialPeltierDehumidifierModel(profile, _calculator);
        var inlet = Inlet(5.0, 0.10, 0.015);
        var result = model.Evaluate(inlet, electricalPowerOverrideW: 5.0);

        Assert.True(result.OutsideValidity);
        Assert.Contains(result.Diagnostics, d => d.Code == "COMMERCIAL_PELTIER.OUTSIDE_VALIDITY");
        // Commanded power may remain outside; map lookup clamps — no extrapolated performance.
        Assert.Equal(5.0, result.ElectricalPowerW, precision: 12);
        Assert.True(result.WaterProductionRateKgPerSecond >= 0.0);
    }

    [Fact]
    public void Component_HumidityDerivedOutlet_ClosesMassAndEnergyBalances()
    {
        // Without independent outlet T/RH, liquid rate equals humidity drop → balances close.
        var profile = CommercialPeltierDehumidifierProfileFitter.FromMapPoints(
            [
                new CommercialPeltierMapPoint
                {
                    InletTemperatureK = UnitConversions.CelsiusToKelvin(28.0),
                    InletRelativeHumidityFraction = 0.55,
                    ElectricalPowerW = 41.0,
                    WaterProductionRateKgPerSecond = 2e-5
                }.Validate()
            ],
            profileId: "balance-map",
            manufacturer: "Test",
            model: "Balance",
            evidenceLevel: TecEvidenceLevel.ProvisionalEngineering,
            sourceIdentifier: "test://balance",
            sourceRevision: "1");

        var component = new CommercialPeltierDehumidifierComponent("commercial", profile, _calculator);
        var inlet = Inlet(28.0, 0.55, 0.015);
        var context = StepContext(inlet, electricalPowerW: 41.0);

        component.Initialize(context.Simulation);
        var step = component.Evaluate(context);
        component.Commit(step);

        Assert.DoesNotContain(step.Diagnostics, d => d.Severity >= Diagnostics.DiagnosticSeverity.Error);
        Assert.Equal(0.0, step.Balance.DryAirMassResidualKg, precision: 10);
        Assert.Equal(0.0, step.Balance.WaterMassResidualKg, precision: 8);
        Assert.Equal(0.0, step.Balance.EnergyResidualJ, precision: 6);
        Assert.Equal(2e-5, component.LastWaterProductionRateKgPerSecond, precision: 12);
        Assert.NotNull(step.OutputStates["outlet"] as MoistAirState);
        Assert.NotNull(step.OutputStates["liquid_out"] as LiquidWaterState);
    }

    [Fact]
    public void Component_SampleFittedProfile_EmitsWaterAndOptionalMismatchDiagnostic()
    {
        var profile = CommercialPeltierDehumidifierProfileFitter.FromPackage(LoadSamplePackage());
        var component = new CommercialPeltierDehumidifierComponent("commercial", profile, _calculator);
        var step = component.Evaluate(StepContext(Inlet(28.2, 0.548, 0.015), 41.4));

        Assert.True(component.LastWaterProductionRateKgPerSecond > 0.0);
        Assert.Equal(0.0, step.Balance.DryAirMassResidualKg, precision: 10);
        Assert.Equal(0.0, step.Balance.EnergyResidualJ, precision: 6);
        // Map outlet T/RH may disagree with map water rate; residual is allowed and diagnosed.
    }

    [Fact]
    public void Kpis_MatchCommonDefinitionsWithAnalyticalPath()
    {
        const double waterRate = 4.1666666666666664e-5;
        const double powerW = 41.4;
        const double coolingW = 80.0;

        var litersPerKwh = CommercialPeltierBlackBoxKpis.LitersPerKwhElectric(waterRate, powerW);
        Assert.NotNull(litersPerKwh);
        Assert.Equal(
            waterRate / (powerW / CommercialPeltierBlackBoxKpis.JoulesPerKilowattHour),
            litersPerKwh!.Value,
            precision: 10);

        var cop = CommercialPeltierBlackBoxKpis.BareCoolingDeviceCop(coolingW, powerW);
        Assert.Equal(coolingW / powerW, cop!.Value, precision: 12);
        Assert.Equal(
            CommercialPeltierBlackBoxKpis.BareCoolingDeviceCopFromEnergy(coolingW * 10.0, powerW * 10.0)!.Value,
            cop.Value,
            precision: 12);
    }

    [Fact]
    public void ProfileWithoutOutlet_UsesHumidityDerivedOutlet()
    {
        var points = new[]
        {
            new CommercialPeltierMapPoint
            {
                InletTemperatureK = UnitConversions.CelsiusToKelvin(30.0),
                InletRelativeHumidityFraction = 0.6,
                ElectricalPowerW = 50.0,
                WaterProductionRateKgPerSecond = 1e-5
            }.Validate()
        };
        var profile = CommercialPeltierDehumidifierProfileFitter.FromMapPoints(
            points,
            profileId: "no-outlet",
            manufacturer: "Test",
            model: "MapOnly",
            evidenceLevel: TecEvidenceLevel.ProvisionalEngineering,
            sourceIdentifier: "test://map",
            sourceRevision: "1");

        Assert.False(profile.SupportsOutletState);
        var model = new CommercialPeltierDehumidifierModel(profile, _calculator);
        var result = model.Evaluate(Inlet(30.0, 0.6, 0.02), 50.0);
        Assert.False(result.OutletStateFromMap);
        Assert.Contains(result.Diagnostics, d => d.Code == "COMMERCIAL_PELTIER.OUTLET_STATE_UNSUPPORTED");
        Assert.True(result.Outlet.HumidityRatioKgPerKgDryAir < Inlet(30.0, 0.6, 0.02).HumidityRatioKgPerKgDryAir);
    }

    private static PrototypeMeasurementPackage LoadSamplePackage()
    {
        var path = FindRepoFile(Path.Combine("samples", "calibration", "prototype-campaign.r3-001.json"));
        return PrototypeWideCsvImporter.ImportPackageFromFiles(path);
    }

    private static ComponentStepContext StepContext(MoistAirState inlet, double electricalPowerW)
        => new()
        {
            Simulation = new SimulationContext
            {
                SimulationStart = DateTimeOffset.Parse("2026-07-15T10:00:00Z"),
                TimeStep = TimeSpan.FromSeconds(1),
                ElapsedTime = TimeSpan.Zero,
                StepIndex = 0
            },
            InputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["inlet"] = inlet,
                ["electrical"] = new ElectricalPowerState { PowerW = electricalPowerW }
            }
        };

    private MoistAirState Inlet(double temperatureC, double rhFraction, double dryAirKgPerSecond)
        => _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(temperatureC),
            PhysicalConstants.StandardAtmosphericPressurePa,
            rhFraction,
            dryAirKgPerSecond);

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
