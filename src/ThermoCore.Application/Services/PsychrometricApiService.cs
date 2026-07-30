using ThermoCore.Api.Contracts;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.Api.Services;

/// <summary>Maps psychrometric HTTP DTOs to Core SI calculations (API-003).</summary>
public sealed class PsychrometricApiService
{
    private readonly IPsychrometricCalculator _calculator;

    public PsychrometricApiService(IPsychrometricCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    public PsychrometricCalculateResponse Calculate(PsychrometricCalculateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RelativeHumidityPercent is < 0.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.RelativeHumidityPercent),
                "Relative humidity percent must be in [0, 100].");
        }

        if (request.AbsolutePressurePa <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.AbsolutePressurePa),
                "Absolute pressure must be positive.");
        }

        var temperatureK = UnitConversions.CelsiusToKelvin(request.TemperatureC);
        var rhFraction = request.RelativeHumidityPercent / 100.0;
        var state = _calculator.CreateFromRelativeHumidity(
            temperatureK,
            request.AbsolutePressurePa,
            rhFraction,
            dryAirMassFlowKgPerSecond: 1.0);

        var dewPointK = _calculator.CalculateDewPointTemperatureK(state.VaporPressurePa);
        return new PsychrometricCalculateResponse
        {
            TemperatureC = request.TemperatureC,
            RelativeHumidityPercent = request.RelativeHumidityPercent,
            AbsolutePressurePa = request.AbsolutePressurePa,
            HumidityRatioKgPerKgDryAir = state.HumidityRatioKgPerKgDryAir,
            DewPointTemperatureC = dewPointK is { } dp
                ? UnitConversions.KelvinToCelsius(dp)
                : null,
            SpecificEnthalpyKJPerKgDryAir = state.SpecificEnthalpyJPerKgDryAir / 1000.0,
            SpecificVolumeM3PerKgDryAir = state.SpecificVolumeM3PerKgDryAir
        };
    }
}
