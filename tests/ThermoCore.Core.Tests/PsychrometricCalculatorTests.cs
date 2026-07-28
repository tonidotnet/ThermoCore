using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Tests;

public class PsychrometricCalculatorTests
{
    private readonly PsychrometricCalculator _calculator = new();

    [Theory]
    [InlineData(0.0, 611.21)]
    [InlineData(20.0, 2338.8)]
    [InlineData(25.0, 3169.0)]
    [InlineData(100.0, 101325.0)]
    public void CalculateSaturationPressurePa_BuckReferenceTemperatures_MatchesExpected(
        double temperatureC,
        double expectedPa)
    {
        var actual = _calculator.CalculateSaturationPressurePa(UnitConversions.CelsiusToKelvin(temperatureC));
        Assert.True(
            Math.Abs(actual - expectedPa) / expectedPa < 0.01,
            $"Expected ~{expectedPa} Pa, got {actual} Pa at {temperatureC} °C.");
    }

    [Fact]
    public void CalculateSaturationPressurePa_ZeroCelsius_Returns611_21()
    {
        var actual = _calculator.CalculateSaturationPressurePa(PhysicalConstants.CelsiusOffsetK);
        Assert.Equal(611.21, actual, precision: 6);
    }

    [Fact]
    public void HumidityRatio_VaporPressure_RoundTrip_PreservesValues()
    {
        const double pressurePa = PhysicalConstants.StandardAtmosphericPressurePa;
        const double vaporPressurePa = 1500.0;

        var humidityRatio = _calculator.CalculateHumidityRatio(pressurePa, vaporPressurePa);
        var vaporBack = _calculator.CalculateVaporPressurePa(pressurePa, humidityRatio);

        Assert.Equal(vaporPressurePa, vaporBack, precision: 9);
    }

    [Theory]
    [InlineData(0.0, 0.30)]
    [InlineData(0.0, 0.50)]
    [InlineData(10.0, 0.40)]
    [InlineData(20.0, 0.50)]
    [InlineData(25.0, 0.30)]
    [InlineData(25.0, 0.50)]
    [InlineData(30.0, 0.40)]
    [InlineData(35.0, 0.50)]
    [InlineData(35.0, 0.90)]
    public void CreateFromRelativeHumidity_ThenFromHumidityRatio_ReproducesRelativeHumidity(
        double temperatureC,
        double relativeHumidityFraction)
    {
        var temperatureK = UnitConversions.CelsiusToKelvin(temperatureC);
        var fromRh = _calculator.CreateFromRelativeHumidity(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction,
            dryAirMassFlowKgPerSecond: 0.01);

        var fromW = _calculator.CreateFromHumidityRatio(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            fromRh.HumidityRatioKgPerKgDryAir,
            dryAirMassFlowKgPerSecond: 0.01);

        Assert.Equal(fromRh.RelativeHumidityFraction, fromW.RelativeHumidityFraction, precision: 8);
        Assert.Equal(fromRh.HumidityRatioKgPerKgDryAir, fromW.HumidityRatioKgPerKgDryAir, precision: 12);
    }

    [Theory]
    [InlineData(0.0, 0.30)]
    [InlineData(20.0, 0.50)]
    [InlineData(25.0, 0.50)]
    [InlineData(35.0, 0.90)]
    public void CalculateDewPointTemperatureK_KnownCases_ReproducesVaporPressure(
        double temperatureC,
        double relativeHumidityFraction)
    {
        var temperatureK = UnitConversions.CelsiusToKelvin(temperatureC);
        var vaporPressurePa = _calculator.CalculateVaporPressureFromRelativeHumidityPa(
            temperatureK,
            relativeHumidityFraction);

        var dewPointK = _calculator.CalculateDewPointTemperatureK(vaporPressurePa);
        Assert.NotNull(dewPointK);

        var reconstructed = _calculator.CalculateSaturationPressurePa(dewPointK.Value);
        Assert.True(
            Math.Abs(vaporPressurePa - reconstructed) <= AbsoluteTolerance.PressurePa,
            $"Expected vapor pressure residual <= {AbsoluteTolerance.PressurePa} Pa, got {Math.Abs(vaporPressurePa - reconstructed)} Pa.");
    }

    [Fact]
    public void CreateFromRelativeHumidity_Saturated_PhaseIsSaturatedAndDewPointEqualsDryBulb()
    {
        var temperatureK = UnitConversions.CelsiusToKelvin(30.0);
        var state = _calculator.CreateFromRelativeHumidity(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction: 1.0,
            dryAirMassFlowKgPerSecond: 0.02);

        Assert.Equal(MoistAirPhaseState.Saturated, state.PhaseState);
        Assert.Equal(1.0, state.RelativeHumidityFraction, precision: 8);
        Assert.Equal(temperatureK, state.DewPointTemperatureK, precision: 3);
    }

    [Fact]
    public void CreateFromHumidityRatio_SupersaturatedRequest_ClassifiesWithoutClamping()
    {
        var temperatureK = UnitConversions.CelsiusToKelvin(20.0);
        var saturationHumidity = _calculator.CalculateHumidityRatio(
            PhysicalConstants.StandardAtmosphericPressurePa,
            _calculator.CalculateSaturationPressurePa(temperatureK));

        var state = _calculator.CreateFromHumidityRatio(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            saturationHumidity * 1.05,
            dryAirMassFlowKgPerSecond: 0.01);

        Assert.Equal(MoistAirPhaseState.SupersaturatedCandidate, state.PhaseState);
        Assert.True(state.RelativeHumidityFraction > 1.0);
    }

    [Fact]
    public void SensibleHeating_PreservesHumidityRatioAndDewPoint()
    {
        var initial = _calculator.CreateFromRelativeHumidity(
            UnitConversions.CelsiusToKelvin(25.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction: 0.50,
            dryAirMassFlowKgPerSecond: 0.01);

        var heated = _calculator.CreateFromHumidityRatio(
            UnitConversions.CelsiusToKelvin(80.0),
            PhysicalConstants.StandardAtmosphericPressurePa,
            initial.HumidityRatioKgPerKgDryAir,
            initial.DryAirMassFlowKgPerSecond);

        Assert.Equal(initial.HumidityRatioKgPerKgDryAir, heated.HumidityRatioKgPerKgDryAir, precision: 12);
        Assert.Equal(initial.WaterVaporMassFlowKgPerSecond, heated.WaterVaporMassFlowKgPerSecond, precision: 12);
        Assert.Equal(initial.DewPointTemperatureK, heated.DewPointTemperatureK, precision: 4);
        Assert.True(heated.RelativeHumidityFraction < initial.RelativeHumidityFraction);
        Assert.True(heated.SpecificEnthalpyJPerKgDryAir > initial.SpecificEnthalpyJPerKgDryAir);
        Assert.Equal(MoistAirPhaseState.Unsaturated, heated.PhaseState);
    }

    [Fact]
    public void Enthalpy_Temperature_RoundTrip_PreservesTemperature()
    {
        var temperatureK = UnitConversions.CelsiusToKelvin(25.0);
        const double humidityRatio = 0.01;
        var enthalpy = _calculator.CalculateSpecificEnthalpyJPerKgDryAir(temperatureK, humidityRatio);
        var recovered = _calculator.CalculateTemperatureKFromEnthalpy(enthalpy, humidityRatio);
        Assert.Equal(temperatureK, recovered, precision: 10);
    }

    [Fact]
    public void SpecificVolumeAndDensity_AreConsistent()
    {
        var temperatureK = UnitConversions.CelsiusToKelvin(25.0);
        const double humidityRatio = 0.01;
        var volume = _calculator.CalculateSpecificVolumeM3PerKgDryAir(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            humidityRatio);
        var density = _calculator.CalculateMoistAirDensityKgPerM3(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            humidityRatio);

        Assert.Equal((1.0 + humidityRatio) / volume, density, precision: 12);
        Assert.True(volume > 0.0);
        Assert.True(density > 0.0);
    }

    [Fact]
    public void CreateFromRelativeHumidity_RelativeHumidityAboveOne_Throws()
    {
        Assert.Throws<PsychrometricInputException>(() =>
            _calculator.CreateFromRelativeHumidity(
                UnitConversions.CelsiusToKelvin(25.0),
                PhysicalConstants.StandardAtmosphericPressurePa,
                relativeHumidityFraction: 1.05,
                dryAirMassFlowKgPerSecond: 0.01));
    }

    [Fact]
    public void CalculateDewPointTemperatureK_NearZeroVaporPressure_ReturnsNull()
    {
        Assert.Null(_calculator.CalculateDewPointTemperatureK(0.0));
    }

    [Fact]
    public void CreateFromDewPoint_MatchesRelativeHumidityPath()
    {
        var temperatureK = UnitConversions.CelsiusToKelvin(25.0);
        var fromRh = _calculator.CreateFromRelativeHumidity(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            relativeHumidityFraction: 0.50,
            dryAirMassFlowKgPerSecond: 0.01);

        var fromDew = _calculator.CreateFromDewPoint(
            temperatureK,
            PhysicalConstants.StandardAtmosphericPressurePa,
            fromRh.DewPointTemperatureK,
            dryAirMassFlowKgPerSecond: 0.01);

        Assert.True(
            Math.Abs(fromRh.VaporPressurePa - fromDew.VaporPressurePa) <= AbsoluteTolerance.PressurePa);
        Assert.True(
            Math.Abs(fromRh.RelativeHumidityFraction - fromDew.RelativeHumidityFraction)
            <= AbsoluteTolerance.RelativeHumidity);
        Assert.Equal(fromRh.PhaseState, fromDew.PhaseState);
    }
}
