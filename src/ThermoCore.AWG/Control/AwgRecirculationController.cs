using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

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
