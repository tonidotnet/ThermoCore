using ThermoCore.Core.Balances;
using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;
using ThermoCore.Core.Units;

var calc = new PsychrometricCalculator();
var parameters = new SilicaGelParameters
{
    DryAdsorbentMassKg = 2.0,
    MaximumWaterLoadingKgPerKgDryAdsorbent = 0.35,
    MinimumRegeneratedLoadingKgPerKgDryAdsorbent = 0.02,
    EffectiveSpecificHeatJPerKgK = 920.0,
    BedHousingThermalCapacityJPerK = 500.0,
    EffectiveHeatOfAdsorptionJPerKgWater = 2_600_000.0,
    BedHeatLossCoefficientWPerK = 0.5,
    ReferenceMassTransferCoefficientPerSecond = 0.02,
    AmbientTemperatureK = UnitConversions.CelsiusToKelvin(25.0),
    AirBedHeatTransferCoefficientWPerK = 80.0
};
var isotherm = GenericPolynomialIsotherm.CreateLinear(parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
var initial = SilicaGelState.Create(
    dryAdsorbentMassKg: parameters.DryAdsorbentMassKg,
    waterLoadingKgPerKgDryAdsorbent: 0.05,
    bedTemperatureK: UnitConversions.CelsiusToKelvin(25.0),
    maximumWaterLoadingKgPerKgDryAdsorbent: parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
    minimumRegeneratedLoadingKgPerKgDryAdsorbent: parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
    effectiveSpecificHeatJPerKgK: parameters.EffectiveSpecificHeatJPerKgK,
    bedHousingThermalCapacityJPerK: parameters.BedHousingThermalCapacityJPerK);
var inlet = calc.CreateFromRelativeHumidity(UnitConversions.CelsiusToKelvin(25.0), PhysicalConstants.StandardAtmosphericPressurePa, 0.70, 0.02);
var source = new AmbientAirSourceComponent("air", inlet);
var bed = new SilicaGelBedComponent("sg", parameters, isotherm, initial, calc);
var sink = new ExhaustAirSinkComponent("sink");
var result = new SimulationEngine().Run(new SimulationRequest
{
    Graph = new SimulationGraph([source, bed, sink], [
        new PhysicalConnection { Id="a", SourceComponentId="air", SourcePortId="outlet", TargetComponentId="sg", TargetPortId="inlet" },
        new PhysicalConnection { Id="b", SourceComponentId="sg", SourcePortId="outlet", TargetComponentId="sink", TargetPortId="inlet" }
    ]),
    StartTimeUtc = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
    Duration = TimeSpan.FromSeconds(5),
    TimeStep = TimeSpan.FromSeconds(5),
    BalanceTolerance = BalanceTolerance.Default with { AbsoluteEnergyJ = 1e6, Relative = 1.0 }
});
Console.WriteLine($"Succeeded={result.Succeeded}");
Console.WriteLine($"EnergyResidualJ={result.Steps[0].SystemBalance.EnergyResidualJ}");
Console.WriteLine($"EnergyIn={result.Steps[0].SystemBalance.EnergyInputJ} Out={result.Steps[0].SystemBalance.EnergyOutputJ} Stor={result.Steps[0].SystemBalance.StoredEnergyChangeJ}");
foreach (var d in result.Diagnostics) Console.WriteLine($"{d.Code} {d.Severity} {d.Message}");
