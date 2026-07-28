using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

Console.WriteLine("ThermoCore Console");
Console.WriteLine($"Standard pressure: {PhysicalConstants.StandardAtmosphericPressurePa} Pa");
Console.WriteLine($"0 °C = {UnitConversions.CelsiusToKelvin(0.0)} K");

var calculator = new PsychrometricCalculator();
var state = calculator.CreateFromRelativeHumidity(
    temperatureK: UnitConversions.CelsiusToKelvin(25.0),
    pressurePa: PhysicalConstants.StandardAtmosphericPressurePa,
    relativeHumidityFraction: 0.50,
    dryAirMassFlowKgPerSecond: 0.01);

Console.WriteLine(
    $"25 °C / 50% RH → W={state.HumidityRatioKgPerKgDryAir:F6} kg/kg, " +
    $"Tdp={UnitConversions.KelvinToCelsius(state.DewPointTemperatureK):F2} °C, " +
    $"h={state.SpecificEnthalpyJPerKgDryAir:F0} J/kg");
