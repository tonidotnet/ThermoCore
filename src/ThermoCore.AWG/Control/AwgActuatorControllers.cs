using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

public enum AwgFanControlStrategy
{
    FixedControlFraction,
    FixedDryAirMassFlow,
    FixedVolumetricFlow,
    PressureControlled,
    OptimizationControlled
}

public enum AwgPeltierControlStrategy
{
    FixedPower,
    MaximumAvailablePower,
    TargetColdSideTemperature,
    TargetDewPointApproach,
    MinimumWhPerLiter,
    ThermalProtectionLimited
}

/// <summary>Resolves fan control fraction from supervisory request and observations (AWG-009).</summary>
public static class AwgFanController
{
    public static double ResolveControlFraction(
        AwgControlRequest request,
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        AwgFanControlStrategy strategy = AwgFanControlStrategy.FixedControlFraction,
        double? targetDryAirMassFlowKgPerSecond = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        if (request.RequestedMode is AwgOperatingMode.Off
            or AwgOperatingMode.Fault
            or AwgOperatingMode.ControlledShutdown)
        {
            return 0.0;
        }

        if (!observation.FanOperatingPointValid)
        {
            return 0.0;
        }

        var fraction = strategy switch
        {
            AwgFanControlStrategy.FixedControlFraction => request.FanControlFraction,
            AwgFanControlStrategy.FixedDryAirMassFlow => ResolveFromMassFlow(
                targetDryAirMassFlowKgPerSecond ?? observation.ProcessDryAirMassFlowKgPerSecond,
                parameters.MinimumSafeDryAirMassFlowKgPerSecond,
                request.FanControlFraction),
            AwgFanControlStrategy.FixedVolumetricFlow => request.FanControlFraction,
            AwgFanControlStrategy.PressureControlled => request.FanControlFraction,
            AwgFanControlStrategy.OptimizationControlled => request.FanControlFraction,
            _ => request.FanControlFraction
        };

        // Minimum safe airflow takes precedence over optimization.
        if (observation.ProcessDryAirMassFlowKgPerSecond < parameters.MinimumSafeDryAirMassFlowKgPerSecond
            && request.RequestedMode is not AwgOperatingMode.Standby)
        {
            fraction = Math.Max(fraction, parameters.NominalFanControlFraction);
        }

        return Math.Clamp(fraction, 0.0, 1.0);
    }

    private static double ResolveFromMassFlow(
        double targetFlow,
        double minimumSafeFlow,
        double fallbackFraction)
    {
        FiniteNumber.RequireNonNegative(targetFlow, nameof(targetFlow));
        if (targetFlow <= 0.0)
        {
            return 0.0;
        }

        if (targetFlow < minimumSafeFlow)
        {
            return 1.0;
        }

        return Math.Clamp(fallbackFraction, 0.0, 1.0);
    }
}

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

/// <summary>Resolves recirculation fraction within configured bounds (AWG-011).</summary>
public static class AwgRecirculationController
{
    public static double ResolveRecirculationFraction(
        AwgControlRequest request,
        AwgSystemObservation observation,
        AwgControlParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        if (!observation.FanOperatingPointValid
            || request.RequestedMode is AwgOperatingMode.Off
                or AwgOperatingMode.Fault
                or AwgOperatingMode.ControlledShutdown
                or AwgOperatingMode.Startup)
        {
            return 0.0;
        }

        if (request.RequestedMode == AwgOperatingMode.Regeneration)
        {
            // Prefer fresh heated air during regeneration.
            return 0.0;
        }

        var fraction = request.RequestedMode == AwgOperatingMode.Recirculation
            ? Math.Max(request.RecirculationFraction, parameters.DefaultRecirculationFraction)
            : request.RecirculationFraction;

        return Math.Clamp(fraction, 0.0, parameters.MaximumRecirculationFraction);
    }
}
