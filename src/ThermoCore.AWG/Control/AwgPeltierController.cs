using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

/// <summary>Resolves Peltier electrical power request (AWG-010).</summary>
public static class AwgPeltierController
{
    public static double ResolvePowerRequestW(
        AwgControlRequest request,
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        AwgPeltierControlStrategy strategy = AwgPeltierControlStrategy.TargetDewPointApproach)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        if (!request.CondenserEnabled || request.PeltierPowerRequestW <= 0.0)
        {
            return 0.0;
        }

        if (observation.PeltierHotSideTemperatureK >= parameters.PeltierHotSideLimitK
            || strategy == AwgPeltierControlStrategy.ThermalProtectionLimited
                && observation.PeltierHotSideTemperatureK
                    >= parameters.PeltierHotSideLimitK - 2.0)
        {
            return 0.0;
        }

        var available = Math.Max(0.0, observation.AvailableElectricalPowerW);
        var nominal = Math.Min(parameters.NominalPeltierPowerRequestW, available);

        var power = strategy switch
        {
            AwgPeltierControlStrategy.FixedPower => Math.Min(request.PeltierPowerRequestW, available),
            AwgPeltierControlStrategy.MaximumAvailablePower => available,
            AwgPeltierControlStrategy.TargetDewPointApproach => ResolveDewPointApproach(
                observation, parameters, nominal),
            AwgPeltierControlStrategy.TargetColdSideTemperature => ResolveDewPointApproach(
                observation, parameters, nominal),
            AwgPeltierControlStrategy.MinimumWhPerLiter => Math.Min(request.PeltierPowerRequestW, available),
            AwgPeltierControlStrategy.ThermalProtectionLimited => Math.Min(request.PeltierPowerRequestW, available),
            _ => Math.Min(request.PeltierPowerRequestW, available)
        };

        if (observation.BatteryStateOfChargeFraction <= parameters.ReserveBatterySocFraction)
        {
            power *= parameters.ReservePeltierDerateFraction;
        }

        return Math.Clamp(power, 0.0, available);
    }

    private static double ResolveDewPointApproach(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        double nominalPowerW)
    {
        var targetSurfaceK =
            observation.CondenserInletDewPointTemperatureK - parameters.TargetDewPointApproachK;
        var errorK = observation.CondenserSurfaceTemperatureK - targetSurfaceK;
        if (errorK <= 0.0)
        {
            // Already at or below target approach; hold a reduced cooling request.
            return 0.35 * nominalPowerW;
        }

        // Proportional request without embedding Peltier physics.
        var gain = Math.Clamp(errorK / Math.Max(parameters.TargetDewPointApproachK, 0.5), 0.0, 1.0);
        return gain * nominalPowerW;
    }
}
