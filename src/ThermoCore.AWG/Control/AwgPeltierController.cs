using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

/// <summary>
/// Resolves Peltier / condenser-cooling electrical power request (AWG-010 / COOL-004).
/// Dew-point tracking: T_surface,target = T_dp,in − configured margin; request the lowest
/// drive that approaches the target inside electrical and thermal limits.
/// </summary>
public static class AwgPeltierController
{
    /// <summary>Backward-compatible power-only resolve.</summary>
    public static double ResolvePowerRequestW(
        AwgControlRequest request,
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        AwgPeltierControlStrategy strategy = AwgPeltierControlStrategy.TargetDewPointApproach,
        double previousPowerRequestW = 0.0,
        TimeSpan? timeStep = null)
        => Resolve(request, observation, parameters, strategy, previousPowerRequestW, timeStep)
            .PowerRequestW;

    public static AwgPeltierControlResult Resolve(
        AwgControlRequest request,
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        AwgPeltierControlStrategy strategy = AwgPeltierControlStrategy.TargetDewPointApproach,
        double previousPowerRequestW = 0.0,
        TimeSpan? timeStep = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();
        FiniteNumber.RequireNonNegative(previousPowerRequestW, nameof(previousPowerRequestW));

        var diagnostics = new List<SimulationDiagnostic>();
        var targetSurfaceK =
            observation.CondenserInletDewPointTemperatureK - parameters.TargetDewPointApproachK;
        var dewPointMarginK =
            observation.CondenserInletDewPointTemperatureK - observation.CondenserSurfaceTemperatureK;

        if (!request.CondenserEnabled || request.PeltierPowerRequestW <= 0.0)
        {
            return new AwgPeltierControlResult
            {
                PowerRequestW = 0.0,
                TargetSurfaceTemperatureK = targetSurfaceK,
                DewPointMarginK = dewPointMarginK,
                PowerSaturated = false,
                TargetUnreachable = false,
                ActiveLimitingConstraint = "condenser-disabled",
                Diagnostics = diagnostics
            };
        }

        if (observation.PeltierHotSideTemperatureK >= parameters.PeltierHotSideLimitK
            || strategy == AwgPeltierControlStrategy.ThermalProtectionLimited
                && observation.PeltierHotSideTemperatureK
                    >= parameters.PeltierHotSideLimitK - 2.0)
        {
            diagnostics.Add(Warning(
                "CTRL.PELTIER_HOT_SIDE_LIMIT",
                "Peltier drive zeroed by hot-side thermal protection."));
            return new AwgPeltierControlResult
            {
                PowerRequestW = 0.0,
                TargetSurfaceTemperatureK = targetSurfaceK,
                DewPointMarginK = dewPointMarginK,
                PowerSaturated = false,
                TargetUnreachable = false,
                ActiveLimitingConstraint = "peltier-hot-side-limit",
                Diagnostics = diagnostics
            };
        }

        var available = Math.Max(0.0, observation.AvailableElectricalPowerW);
        var maxPower = ResolveMaximumPowerW(parameters, available);
        var minPower = Math.Clamp(parameters.MinimumPeltierPowerRequestW, 0.0, maxPower);
        // Strategy uses nominal against available bus power; hard max/current caps apply after.
        var nominal = Math.Min(parameters.NominalPeltierPowerRequestW, available);

        var unconstrained = strategy switch
        {
            AwgPeltierControlStrategy.FixedPower => Math.Min(request.PeltierPowerRequestW, available),
            AwgPeltierControlStrategy.MaximumAvailablePower => available,
            AwgPeltierControlStrategy.TargetDewPointApproach => ResolveDewPointApproach(
                observation, parameters, nominal, targetSurfaceK, diagnostics),
            AwgPeltierControlStrategy.TargetColdSideTemperature => ResolveDewPointApproach(
                observation, parameters, nominal, targetSurfaceK, diagnostics),
            AwgPeltierControlStrategy.MinimumWhPerLiter => Math.Min(request.PeltierPowerRequestW, available),
            AwgPeltierControlStrategy.ThermalProtectionLimited => Math.Min(request.PeltierPowerRequestW, available),
            _ => Math.Min(request.PeltierPowerRequestW, available)
        };

        if (observation.BatteryStateOfChargeFraction <= parameters.ReserveBatterySocFraction)
        {
            unconstrained *= parameters.ReservePeltierDerateFraction;
        }

        // Enforce min when actively cooling toward a warm surface (error > 0).
        var errorK = observation.CondenserSurfaceTemperatureK - targetSurfaceK;
        if (errorK > 0.0 && unconstrained > 0.0)
        {
            unconstrained = Math.Max(unconstrained, minPower);
        }

        var limited = Math.Clamp(unconstrained, 0.0, maxPower);
        var constraint = "none";
        var saturated = unconstrained > maxPower + 1e-9;

        if (saturated)
        {
            constraint = maxPower + 1e-12 < available
                ? "maximum-power"
                : "available-electrical-power";
            diagnostics.Add(Warning(
                "CTRL.PELTIER_POWER_SATURATED",
                $"Peltier request saturated at {maxPower:G4} W."));
        }

        var targetUnreachable = false;
        if (targetSurfaceK < parameters.MinimumCondenserSurfaceTemperatureK
            && observation.CondenserSurfaceTemperatureK > targetSurfaceK + 0.25)
        {
            targetUnreachable = true;
            constraint = constraint == "none" ? "minimum-surface-temperature" : constraint;
            diagnostics.Add(Warning(
                "CTRL.PELTIER_TARGET_UNREACHABLE",
                "Dew-point approach target is below the minimum allowed condenser surface temperature."));
        }

        // Anti-chatter / slew-rate limit (disabled when ramp is non-finite).
        var ramp = parameters.PeltierPowerRampLimitWPerSecond;
        if (double.IsFinite(ramp)
            && ramp > 0.0
            && timeStep is { } dt
            && dt.TotalSeconds > 0.0)
        {
            var maxStep = ramp * dt.TotalSeconds;
            var delta = limited - previousPowerRequestW;
            if (Math.Abs(delta) > maxStep)
            {
                limited = previousPowerRequestW + Math.Sign(delta) * maxStep;
                limited = Math.Clamp(limited, 0.0, maxPower);
                constraint = "power-ramp-limit";
                diagnostics.Add(Info(
                    "CTRL.PELTIER_RAMP_LIMIT",
                    $"Peltier power slew limited to {ramp:G4} W/s."));
            }
        }

        return new AwgPeltierControlResult
        {
            PowerRequestW = limited,
            TargetSurfaceTemperatureK = targetSurfaceK,
            DewPointMarginK = dewPointMarginK,
            PowerSaturated = saturated,
            TargetUnreachable = targetUnreachable,
            ActiveLimitingConstraint = constraint,
            Diagnostics = diagnostics
        };
    }

    private static double ResolveMaximumPowerW(AwgControlParameters parameters, double availableW)
    {
        var configuredMax = parameters.MaximumPeltierPowerRequestW
            ?? parameters.NominalPeltierPowerRequestW;

        if (parameters.MaximumPeltierCurrentA is { } imax
            && parameters.TecOperatingVoltageV is { } voltage
            && imax > 0.0
            && voltage > 0.0)
        {
            configuredMax = Math.Min(configuredMax, imax * voltage);
        }

        return Math.Min(configuredMax, availableW);
    }

    private static double ResolveDewPointApproach(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        double nominalPowerW,
        double targetSurfaceK,
        List<SimulationDiagnostic> diagnostics)
    {
        // Clamp the commanded target to the physically allowed surface floor.
        var attainableTargetK = Math.Max(targetSurfaceK, parameters.MinimumCondenserSurfaceTemperatureK);
        var errorK = observation.CondenserSurfaceTemperatureK - attainableTargetK;
        if (errorK <= 0.0)
        {
            // Already at or below target approach; hold a reduced cooling request.
            return parameters.HoldPowerFractionWhenAtOrBelowTarget * nominalPowerW;
        }

        // Higher inlet dew point ⇒ lower temperature lift / smaller error for the same surface
        // ⇒ lower proportional drive (water/energy objective, not minimum cold temperature).
        var gain = Math.Clamp(
            errorK / Math.Max(parameters.TargetDewPointApproachK, 0.5),
            0.0,
            1.0);
        return gain * nominalPowerW;
    }

    private static SimulationDiagnostic Warning(string code, string message)
        => new() { Code = code, Severity = DiagnosticSeverity.Warning, Message = message };

    private static SimulationDiagnostic Info(string code, string message)
        => new() { Code = code, Severity = DiagnosticSeverity.Information, Message = message };
}
