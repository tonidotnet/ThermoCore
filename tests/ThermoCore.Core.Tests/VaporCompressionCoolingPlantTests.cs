using ThermoCore.Core.Components.VaporCompression;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class VaporCompressionCoolingPlantTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Fact]
    public void Plant_AtMapPoint_ProducesWaterAndClosesBalances()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference();
        var plant = new VaporCompressionCoolingPlantModel(map, calculator: _calculator);
        var point = map.MapPoints.First(p => p.SpeedFraction >= 0.999);

        var result = plant.Evaluate(new VaporCompressionPlantStepRequest
        {
            Inlet = Inlet(30.0, 0.70, 0.02),
            Simulation = Sim(TimeSpan.FromSeconds(1), 0),
            EvaporatingTemperatureK = point.EvaporatingTemperatureK,
            CondensingTemperatureK = point.CondensingTemperatureK,
            SpeedFraction = point.SpeedFraction,
            CompressorRequested = true,
            ProcessFanElectricalPowerW = 10.0
        });

        Assert.True(result.CompressorOn);
        Assert.True(result.CoolingDeliveredW > 0.0);
        Assert.True(result.ElectricalInputW > 0.0);
        Assert.Equal(0.0, result.Balance.DryAirMassResidualKg, precision: 8);
        Assert.Equal(0.0, result.Balance.WaterMassResidualKg, precision: 8);
        Assert.Equal(0.0, result.Balance.EnergyResidualJ, precision: 4);
        Assert.NotNull(result.BareDeviceCop);
    }

    [Fact]
    public void Cycling_EnforcesMinimumRuntime_Deterministically()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference() with
        {
            Cycling = new VaporCompressionCyclingLimits
            {
                MinimumRuntime = TimeSpan.FromSeconds(3),
                MinimumOffTime = TimeSpan.FromSeconds(2)
            }
        };
        var plant = new VaporCompressionCoolingPlantModel(map, calculator: _calculator);
        var te = UnitConversions.CelsiusToKelvin(10.0);
        var tc = UnitConversions.CelsiusToKelvin(35.0);
        var dt = TimeSpan.FromSeconds(1);

        var on = plant.Evaluate(Req(te, tc, requested: true, dt, step: 0));
        Assert.True(on.CompressorOn);

        var hold = plant.Evaluate(Req(te, tc, requested: false, dt, step: 1));
        Assert.True(hold.CompressorOn);
        Assert.True(hold.HeldByCyclingLimits);
        Assert.Contains(hold.Diagnostics, d => d.Code == "VC.CYCLING_MIN_RUNTIME");

        plant.Evaluate(Req(te, tc, requested: false, dt, step: 2));
        var off = plant.Evaluate(Req(te, tc, requested: false, dt, step: 3));
        Assert.False(off.CompressorOn);
        Assert.Equal(0.0, off.ElectricalInputW, precision: 12);
    }

    [Fact]
    public void Cycling_EnforcesMinimumOffTime_BeforeRestart()
    {
        var map = VaporCompressionPerformanceMapCatalog.CreateGenericSmallDcModuleReference() with
        {
            Cycling = new VaporCompressionCyclingLimits
            {
                MinimumRuntime = TimeSpan.FromSeconds(1),
                MinimumOffTime = TimeSpan.FromSeconds(3)
            }
        };
        var plant = new VaporCompressionCoolingPlantModel(map, calculator: _calculator);
        var te = UnitConversions.CelsiusToKelvin(10.0);
        var tc = UnitConversions.CelsiusToKelvin(35.0);
        var dt = TimeSpan.FromSeconds(1);

        plant.Evaluate(Req(te, tc, requested: true, dt, step: 0));
        plant.Evaluate(Req(te, tc, requested: false, dt, step: 1)); // runtime satisfied → off
        Assert.False(plant.CompressorIsOn);

        var heldOff = plant.Evaluate(Req(te, tc, requested: true, dt, step: 2));
        Assert.False(heldOff.CompressorOn);
        Assert.Contains(heldOff.Diagnostics, d => d.Code == "VC.CYCLING_MIN_OFF_TIME");

        plant.Evaluate(Req(te, tc, requested: true, dt, step: 3));
        var restarted = plant.Evaluate(Req(te, tc, requested: true, dt, step: 4));
        Assert.True(restarted.CompressorOn);
    }

    [Fact]
    public void ControllerAlone_IsDeterministicAcrossIdenticalSequences()
    {
        var limits = new VaporCompressionCyclingLimits
        {
            MinimumRuntime = TimeSpan.FromSeconds(2),
            MinimumOffTime = TimeSpan.FromSeconds(2)
        };

        bool[] Sequence(VaporCompressionCyclingController c)
        {
            return
            [
                c.Step(true, TimeSpan.FromSeconds(1)).CompressorOn,
                c.Step(false, TimeSpan.FromSeconds(1)).CompressorOn,
                c.Step(false, TimeSpan.FromSeconds(1)).CompressorOn,
                c.Step(true, TimeSpan.FromSeconds(1)).CompressorOn,
                c.Step(true, TimeSpan.FromSeconds(1)).CompressorOn
            ];
        }

        var a = Sequence(new VaporCompressionCyclingController(limits));
        var b = Sequence(new VaporCompressionCyclingController(limits));
        Assert.Equal(a, b);
        Assert.Equal(new[] { true, true, false, false, true }, a);
    }

    private VaporCompressionPlantStepRequest Req(
        double tevapK,
        double tcondK,
        bool requested,
        TimeSpan dt,
        int step)
        => new()
        {
            Inlet = Inlet(30.0, 0.65, 0.02),
            Simulation = Sim(dt, step),
            EvaporatingTemperatureK = tevapK,
            CondensingTemperatureK = tcondK,
            SpeedFraction = 1.0,
            CompressorRequested = requested
        };

    private MoistAirState Inlet(double temperatureC, double rh, double flow)
        => _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(temperatureC),
            PhysicalConstants.StandardAtmosphericPressurePa,
            rh,
            flow);

    private static SimulationContext Sim(TimeSpan dt, int step)
        => new()
        {
            SimulationStart = DateTimeOffset.Parse("2026-07-15T10:00:00Z"),
            TimeStep = dt,
            ElapsedTime = TimeSpan.FromSeconds(dt.TotalSeconds * step),
            StepIndex = step
        };
}
