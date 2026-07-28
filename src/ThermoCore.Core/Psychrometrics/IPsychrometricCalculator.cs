namespace ThermoCore.Core.Psychrometrics;

public interface IPsychrometricCalculator
{
    MoistAirState CreateFromRelativeHumidity(
        double temperatureK,
        double pressurePa,
        double relativeHumidityFraction,
        double dryAirMassFlowKgPerSecond);

    MoistAirState CreateFromHumidityRatio(
        double temperatureK,
        double pressurePa,
        double humidityRatioKgPerKgDryAir,
        double dryAirMassFlowKgPerSecond);

    MoistAirState CreateFromDewPoint(
        double temperatureK,
        double pressurePa,
        double dewPointTemperatureK,
        double dryAirMassFlowKgPerSecond);

    double CalculateSaturationPressurePa(double temperatureK);

    double CalculateHumidityRatio(double pressurePa, double vaporPressurePa);

    double CalculateVaporPressurePa(double pressurePa, double humidityRatioKgPerKgDryAir);

    double CalculateVaporPressureFromRelativeHumidityPa(double temperatureK, double relativeHumidityFraction);

    double? CalculateDewPointTemperatureK(double vaporPressurePa);

    double CalculateSpecificEnthalpyJPerKgDryAir(double temperatureK, double humidityRatioKgPerKgDryAir);

    double CalculateTemperatureKFromEnthalpy(double specificEnthalpyJPerKgDryAir, double humidityRatioKgPerKgDryAir);

    double CalculateSpecificVolumeM3PerKgDryAir(double temperatureK, double pressurePa, double humidityRatioKgPerKgDryAir);

    double CalculateMoistAirDensityKgPerM3(double temperatureK, double pressurePa, double humidityRatioKgPerKgDryAir);
}
