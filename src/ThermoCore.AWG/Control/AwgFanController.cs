using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

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
