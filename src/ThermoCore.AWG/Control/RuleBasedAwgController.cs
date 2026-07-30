using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Units;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

/// <summary>
/// Deterministic rule-based AWG supervisory controller
/// (docs/04_Simulation/14_ControlSystem.md / AWG-008).
/// </summary>
public sealed class RuleBasedAwgController : IAwgController
{
    public AwgControlStepResult Evaluate(
        AwgSystemObservation observation,
        AwgControllerState currentState,
        AwgControlParameters parameters,
        TimeSpan timeStep)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentNullException.ThrowIfNull(parameters);
        FiniteNumber.RequirePositive(timeStep.TotalSeconds, nameof(timeStep));

        observation.Validate();
        parameters.Validate();

        var diagnostics = new List<SimulationDiagnostic>();
        var trace = new List<AwgDecisionTraceEntry>();
        var scalars = CaptureScalars(observation, parameters);

        if (currentState.IsLatchedFault)
        {
            return BuildResult(
                observation,
                currentState,
                parameters,
                AwgOperatingMode.Fault,
                "LATCHED_FAULT",
                "latched-fault",
                AwgFaultCode.ComponentCriticalDiagnostic,
                timeStep,
                diagnostics,
                trace,
                scalars,
                forceTransition: false);
        }

        var safety = EvaluateSafety(observation, parameters, diagnostics);
        if (safety is not null)
        {
            var faultCount = currentState.ConsecutiveFaultCount + 1;
            var latch = faultCount >= parameters.FaultLatchThreshold
                || safety.Value.Mode == AwgOperatingMode.Fault;
            var proposed = new AwgControllerState
            {
                CurrentMode = safety.Value.Mode,
                TimeInCurrentMode = safety.Value.Mode == currentState.CurrentMode
                    ? currentState.TimeInCurrentMode + timeStep
                    : timeStep,
                LastModeChangeUtc = safety.Value.Mode == currentState.CurrentMode
                    ? currentState.LastModeChangeUtc
                    : observation.SimulationTimeUtc,
                ConsecutiveFaultCount = faultCount,
                IsLatchedFault = latch && safety.Value.Mode == AwgOperatingMode.Fault,
                LastTransitionReasonCode = safety.Value.ReasonCode,
                ActiveFaultCode = safety.Value.FaultCode
            };

            trace.Add(new AwgDecisionTraceEntry
            {
                ReasonCode = safety.Value.ReasonCode,
                PreviousMode = currentState.CurrentMode.ToString(),
                RequestedMode = safety.Value.Mode.ToString(),
                ActiveLimitingConstraint = safety.Value.Constraint,
                ScalarInputs = scalars
            });

            return new AwgControlStepResult
            {
                Request = BuildRequest(
                    safety.Value.Mode,
                    observation,
                    parameters,
                    safety.Value.ReasonCode,
                    safety.Value.FaultCode,
                    waterTankFull: observation.WaterTankLevelFraction >= 1.0,
                    reserveSoc: observation.BatteryStateOfChargeFraction <= parameters.ReserveBatterySocFraction),
                ProposedState = proposed,
                Diagnostics = diagnostics,
                DecisionTrace = trace
            };
        }

        var selected = SelectMode(observation, currentState, parameters, diagnostics);
        var dwellBlocks =
            selected.Mode != currentState.CurrentMode
            && currentState.CurrentMode is not (AwgOperatingMode.Off or AwgOperatingMode.Startup)
            && currentState.TimeInCurrentMode < parameters.MinimumModeDwell
            && !selected.BypassDwell;

        if (dwellBlocks)
        {
            selected = selected with
            {
                Mode = currentState.CurrentMode,
                ReasonCode = "DWELL_HOLD",
                Constraint = "minimum-mode-dwell"
            };
            diagnostics.Add(Info("CTRL.DWELL_HOLD", "Mode transition deferred by minimum dwell time."));
        }

        trace.Add(new AwgDecisionTraceEntry
        {
            ReasonCode = selected.ReasonCode,
            PreviousMode = currentState.CurrentMode.ToString(),
            RequestedMode = selected.Mode.ToString(),
            ActiveLimitingConstraint = selected.Constraint,
            ScalarInputs = scalars
        });

        return BuildResult(
            observation,
            currentState,
            parameters,
            selected.Mode,
            selected.ReasonCode,
            selected.Constraint,
            AwgFaultCode.None,
            timeStep,
            diagnostics,
            trace,
            scalars,
            forceTransition: selected.Mode != currentState.CurrentMode);
    }

    public static AwgControlParameters CreateDefaultParameters()
    {
        var parameters = new AwgControlParameters
        {
            AdsorptionTargetLoadingKgPerKg = 0.20,
            RegenerationEntryLoadingKgPerKg = 0.20,
            RegenerationExitLoadingKgPerKg = 0.08,
            MinimumAdsorptionDrivingForceKgPerKg = 0.02,
            CondensationDewPointMarginK = 2.0,
            TargetDewPointApproachK = 3.0,
            MaximumRecirculationFraction = 0.5,
            ReserveBatterySocFraction = 0.25,
            CriticalBatterySocFraction = 0.10,
            MinimumModeDwell = TimeSpan.FromMinutes(5),
            PeltierHotSideLimitK = UnitConversions.CelsiusToKelvin(70.0),
            SilicaGelTemperatureLimitK = UnitConversions.CelsiusToKelvin(95.0),
            CollectorAbsorberTemperatureLimitK = UnitConversions.CelsiusToKelvin(120.0),
            MinimumSafeDryAirMassFlowKgPerSecond = 0.005,
            NominalFanControlFraction = 1.0,
            NominalPeltierPowerRequestW = 120.0,
            MinimumSolarIrradianceForRegenerationWPerSquareMeter = 250.0,
            ReservePeltierDerateFraction = 0.25,
            DefaultRecirculationFraction = 0.0,
            FaultLatchThreshold = 1
        };
        return parameters.Validate();
    }

    private static (AwgOperatingMode Mode, string ReasonCode, string Constraint, AwgFaultCode FaultCode)? EvaluateSafety(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        List<SimulationDiagnostic> diagnostics)
    {
        if (observation.ComponentDiagnostics.Any(d =>
                d.Severity is DiagnosticSeverity.Critical
                && !string.Equals(d.Code, "POWER.SOLAR_CURTAILED", StringComparison.Ordinal)))
        {
            diagnostics.Add(Error("CTRL.COMPONENT_CRITICAL", "Critical component diagnostic forces Fault."));
            return (AwgOperatingMode.Fault, "COMPONENT_CRITICAL", "component-critical-diagnostic", AwgFaultCode.ComponentCriticalDiagnostic);
        }

        if (!observation.FanOperatingPointValid)
        {
            diagnostics.Add(Error("CTRL.FAN_INVALID", "Fan operating point unavailable."));
            return (AwgOperatingMode.Fault, "FAN_OPERATING_POINT_UNAVAILABLE", "fan-operating-point", AwgFaultCode.FanOperatingPointUnavailable);
        }

        if (observation.BatteryStateOfChargeFraction <= parameters.CriticalBatterySocFraction)
        {
            diagnostics.Add(Error("CTRL.BATTERY_CRITICAL", "Battery below critical SOC."));
            return (AwgOperatingMode.ControlledShutdown, "BATTERY_BELOW_CRITICAL_SOC", "battery-critical-soc", AwgFaultCode.BatteryBelowCriticalSoc);
        }

        if (observation.PeltierHotSideTemperatureK >= parameters.PeltierHotSideLimitK)
        {
            diagnostics.Add(Error("CTRL.PELTIER_HOT", "Peltier hot-side overtemperature."));
            return (AwgOperatingMode.Fault, "PELTIER_HOT_SIDE_OVERTEMPERATURE", "peltier-hot-side", AwgFaultCode.PeltierHotSideOverTemperature);
        }

        if (observation.SilicaGelTemperatureK >= parameters.SilicaGelTemperatureLimitK)
        {
            diagnostics.Add(Error("CTRL.SILICA_OVERTEMP", "Silica-gel bed overtemperature."));
            return (AwgOperatingMode.Fault, "SILICA_GEL_OVERTEMPERATURE", "silica-gel-temperature", AwgFaultCode.SilicaGelOverTemperature);
        }

        if (observation.CollectorAbsorberTemperatureK >= parameters.CollectorAbsorberTemperatureLimitK)
        {
            diagnostics.Add(Error("CTRL.COLLECTOR_OVERTEMP", "Collector absorber overtemperature."));
            return (AwgOperatingMode.Fault, "COLLECTOR_OVERTEMPERATURE", "collector-absorber-temperature", AwgFaultCode.CollectorOverTemperature);
        }

        if (observation.ProcessDryAirMassFlowKgPerSecond < parameters.MinimumSafeDryAirMassFlowKgPerSecond
            && observation.BatteryStateOfChargeFraction > parameters.ReserveBatterySocFraction)
        {
            // Insufficient airflow is a soft fault only when power is available to run the fan.
            diagnostics.Add(Warning("CTRL.AIRFLOW_LOW", "Process airflow below minimum safe mass flow."));
        }

        return null;
    }

    private static ModeDecision SelectMode(
        AwgSystemObservation observation,
        AwgControllerState currentState,
        AwgControlParameters parameters,
        List<SimulationDiagnostic> diagnostics)
    {
        var reserve = observation.BatteryStateOfChargeFraction <= parameters.ReserveBatterySocFraction;
        var tankFull = observation.WaterTankLevelFraction >= 1.0;
        if (tankFull)
        {
            diagnostics.Add(Warning("CTRL.WATER_TANK_FULL", "Water tank is full; condensation disabled."));
        }

        return currentState.CurrentMode switch
        {
            AwgOperatingMode.Off => new ModeDecision(
                AwgOperatingMode.Startup,
                "ENTER_STARTUP",
                "startup-sequence",
                BypassDwell: true),

            AwgOperatingMode.Startup => SelectAfterStartup(observation, parameters, reserve),

            AwgOperatingMode.Adsorption => SelectFromAdsorption(observation, parameters, reserve, tankFull),

            AwgOperatingMode.Regeneration => SelectFromRegeneration(observation, parameters, reserve, tankFull),

            AwgOperatingMode.Condensation => SelectFromCondensation(observation, parameters, reserve, tankFull),

            AwgOperatingMode.Standby => SelectFromStandby(observation, parameters, reserve, tankFull),

            AwgOperatingMode.ControlledShutdown => new ModeDecision(
                AwgOperatingMode.Off,
                "SHUTDOWN_COMPLETE",
                "controlled-shutdown",
                BypassDwell: true),

            AwgOperatingMode.HeatRecovery => SelectFromStandby(observation, parameters, reserve, tankFull),

            AwgOperatingMode.Recirculation => SelectFromStandby(observation, parameters, reserve, tankFull),

            AwgOperatingMode.Fault => new ModeDecision(
                AwgOperatingMode.Fault,
                "FAULT_HOLD",
                "fault",
                BypassDwell: true),

            _ => new ModeDecision(
                AwgOperatingMode.Standby,
                "FALLBACK_STANDBY",
                "fallback",
                BypassDwell: true)
        };
    }

    private static ModeDecision SelectAfterStartup(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        bool reserve)
    {
        if (reserve)
        {
            return new ModeDecision(AwgOperatingMode.Standby, "STARTUP_TO_STANDBY_RESERVE", "battery-reserve", true);
        }

        if (CanEnterAdsorption(observation, parameters))
        {
            return new ModeDecision(AwgOperatingMode.Adsorption, "STARTUP_TO_ADSORPTION", "adsorption-entry", true);
        }

        if (CanEnterRegeneration(observation, parameters))
        {
            return new ModeDecision(AwgOperatingMode.Regeneration, "STARTUP_TO_REGENERATION", "regeneration-entry", true);
        }

        return new ModeDecision(AwgOperatingMode.Standby, "STARTUP_TO_STANDBY", "no-process-opportunity", true);
    }

    private static ModeDecision SelectFromAdsorption(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        bool reserve,
        bool tankFull)
    {
        if (reserve)
        {
            return new ModeDecision(AwgOperatingMode.Standby, "ADSORPTION_EXIT_RESERVE", "battery-reserve");
        }

        if (observation.SilicaGelLoadingKgPerKg >= parameters.AdsorptionTargetLoadingKgPerKg
            || observation.SilicaGelLoadingKgPerKg >= parameters.RegenerationEntryLoadingKgPerKg)
        {
            if (CanEnterRegeneration(observation, parameters))
            {
                return new ModeDecision(AwgOperatingMode.Regeneration, "ADSORPTION_TO_REGENERATION", "loading-target");
            }

            return new ModeDecision(AwgOperatingMode.Standby, "ADSORPTION_TARGET_STANDBY", "loading-target-no-heat");
        }

        if (!CanEnterAdsorption(observation, parameters))
        {
            return new ModeDecision(AwgOperatingMode.Standby, "ADSORPTION_EXIT_DRIVING_FORCE", "adsorption-driving-force");
        }

        if (!tankFull && CanCondense(observation, parameters) && observation.AvailableElectricalPowerW > 0.0)
        {
            // Condensation may run in parallel conceptually, but MVP uses Condensation as a dedicated mode
            // only when regeneration humidity is being harvested. Stay in adsorption otherwise.
        }

        return new ModeDecision(AwgOperatingMode.Adsorption, "ADSORPTION_HOLD", "adsorption-hold");
    }

    private static ModeDecision SelectFromRegeneration(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        bool reserve,
        bool tankFull)
    {
        if (reserve)
        {
            return new ModeDecision(AwgOperatingMode.Standby, "REGENERATION_EXIT_RESERVE", "battery-reserve");
        }

        if (observation.SilicaGelLoadingKgPerKg <= parameters.RegenerationExitLoadingKgPerKg)
        {
            if (!tankFull && CanCondense(observation, parameters))
            {
                return new ModeDecision(AwgOperatingMode.Condensation, "REGENERATION_TO_CONDENSATION", "regeneration-exit");
            }

            if (CanEnterAdsorption(observation, parameters))
            {
                return new ModeDecision(AwgOperatingMode.Adsorption, "REGENERATION_TO_ADSORPTION", "regeneration-exit");
            }

            return new ModeDecision(AwgOperatingMode.Standby, "REGENERATION_EXIT_STANDBY", "regeneration-exit");
        }

        if (!CanContinueRegeneration(observation, parameters))
        {
            return new ModeDecision(AwgOperatingMode.Standby, "REGENERATION_HEAT_UNAVAILABLE", "solar-heat-unavailable");
        }

        return new ModeDecision(AwgOperatingMode.Regeneration, "REGENERATION_HOLD", "regeneration-hold");
    }

    private static ModeDecision SelectFromCondensation(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        bool reserve,
        bool tankFull)
    {
        if (reserve)
        {
            return new ModeDecision(AwgOperatingMode.Standby, "CONDENSATION_EXIT_RESERVE", "battery-reserve");
        }

        if (tankFull || !CanCondense(observation, parameters))
        {
            if (CanEnterAdsorption(observation, parameters))
            {
                return new ModeDecision(AwgOperatingMode.Adsorption, "CONDENSATION_TO_ADSORPTION", "condensation-exit");
            }

            return new ModeDecision(AwgOperatingMode.Standby, "CONDENSATION_EXIT_STANDBY", "condensation-exit");
        }

        return new ModeDecision(AwgOperatingMode.Condensation, "CONDENSATION_HOLD", "condensation-hold");
    }

    private static ModeDecision SelectFromStandby(
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        bool reserve,
        bool tankFull)
    {
        if (reserve)
        {
            return new ModeDecision(AwgOperatingMode.Standby, "STANDBY_HOLD_RESERVE", "battery-reserve");
        }

        if (CanEnterRegeneration(observation, parameters))
        {
            return new ModeDecision(AwgOperatingMode.Regeneration, "STANDBY_TO_REGENERATION", "regeneration-entry");
        }

        if (CanEnterAdsorption(observation, parameters))
        {
            return new ModeDecision(AwgOperatingMode.Adsorption, "STANDBY_TO_ADSORPTION", "adsorption-entry");
        }

        if (!tankFull && CanCondense(observation, parameters) && observation.AvailableElectricalPowerW > 0.0)
        {
            return new ModeDecision(AwgOperatingMode.Condensation, "STANDBY_TO_CONDENSATION", "condensation-entry");
        }

        return new ModeDecision(AwgOperatingMode.Standby, "STANDBY_HOLD", "standby-hold");
    }

    private static bool CanEnterAdsorption(AwgSystemObservation observation, AwgControlParameters parameters)
    {
        var drivingForce =
            observation.SilicaGelEquilibriumLoadingKgPerKg - observation.SilicaGelLoadingKgPerKg;
        return observation.SilicaGelLoadingKgPerKg < parameters.AdsorptionTargetLoadingKgPerKg
            && drivingForce > parameters.MinimumAdsorptionDrivingForceKgPerKg
            && observation.ProcessDryAirMassFlowKgPerSecond >= parameters.MinimumSafeDryAirMassFlowKgPerSecond
            && observation.AmbientVaporPressurePa > 0.0
            && observation.FanOperatingPointValid;
    }

    private static bool CanEnterRegeneration(AwgSystemObservation observation, AwgControlParameters parameters)
        => observation.SilicaGelLoadingKgPerKg >= parameters.RegenerationEntryLoadingKgPerKg
            && CanContinueRegeneration(observation, parameters);

    private static bool CanContinueRegeneration(AwgSystemObservation observation, AwgControlParameters parameters)
        => observation.SolarIrradianceWPerSquareMeter
                >= parameters.MinimumSolarIrradianceForRegenerationWPerSquareMeter
            && observation.ProcessDryAirMassFlowKgPerSecond >= parameters.MinimumSafeDryAirMassFlowKgPerSecond
            && observation.FanOperatingPointValid;

    private static bool CanCondense(AwgSystemObservation observation, AwgControlParameters parameters)
        => observation.CondenserSurfaceTemperatureK
            < observation.CondenserInletDewPointTemperatureK - parameters.CondensationDewPointMarginK
            && observation.AvailableElectricalPowerW > 0.0;

    private static AwgControlStepResult BuildResult(
        AwgSystemObservation observation,
        AwgControllerState currentState,
        AwgControlParameters parameters,
        AwgOperatingMode mode,
        string reasonCode,
        string constraint,
        AwgFaultCode faultCode,
        TimeSpan timeStep,
        List<SimulationDiagnostic> diagnostics,
        List<AwgDecisionTraceEntry> trace,
        IReadOnlyDictionary<string, double> scalars,
        bool forceTransition)
    {
        _ = constraint;
        _ = scalars;
        _ = forceTransition;

        var modeChanged = mode != currentState.CurrentMode;
        var proposed = new AwgControllerState
        {
            CurrentMode = mode,
            TimeInCurrentMode = modeChanged ? timeStep : currentState.TimeInCurrentMode + timeStep,
            LastModeChangeUtc = modeChanged ? observation.SimulationTimeUtc : currentState.LastModeChangeUtc,
            ConsecutiveFaultCount = faultCode == AwgFaultCode.None ? 0 : currentState.ConsecutiveFaultCount,
            IsLatchedFault = currentState.IsLatchedFault,
            LastTransitionReasonCode = reasonCode,
            ActiveFaultCode = faultCode
        };

        return new AwgControlStepResult
        {
            Request = BuildRequest(
                mode,
                observation,
                parameters,
                reasonCode,
                faultCode,
                waterTankFull: observation.WaterTankLevelFraction >= 1.0,
                reserveSoc: observation.BatteryStateOfChargeFraction <= parameters.ReserveBatterySocFraction),
            ProposedState = proposed,
            Diagnostics = diagnostics,
            DecisionTrace = trace
        };
    }

    private static AwgControlRequest BuildRequest(
        AwgOperatingMode mode,
        AwgSystemObservation observation,
        AwgControlParameters parameters,
        string reasonCode,
        AwgFaultCode faultCode,
        bool waterTankFull,
        bool reserveSoc)
    {
        var fan = mode is AwgOperatingMode.Off or AwgOperatingMode.Fault or AwgOperatingMode.ControlledShutdown
            ? 0.0
            : Math.Clamp(parameters.NominalFanControlFraction, 0.0, 1.0);

        if (mode is AwgOperatingMode.Standby && reserveSoc)
        {
            fan = Math.Min(fan, 0.3);
        }

        var condenserEnabled =
            !waterTankFull
            && mode is AwgOperatingMode.Condensation or AwgOperatingMode.Regeneration
            && CanCondense(observation, parameters);

        var peltier = 0.0;
        if (condenserEnabled)
        {
            peltier = Math.Min(parameters.NominalPeltierPowerRequestW, observation.AvailableElectricalPowerW);
            if (reserveSoc)
            {
                peltier *= parameters.ReservePeltierDerateFraction;
            }
        }

        var recirculation = Math.Clamp(
            mode is AwgOperatingMode.Recirculation
                ? parameters.DefaultRecirculationFraction
                : 0.0,
            0.0,
            parameters.MaximumRecirculationFraction);

        return new AwgControlRequest
        {
            RequestedMode = mode,
            FanControlFraction = fan,
            PeltierPowerRequestW = peltier,
            RecirculationFraction = recirculation,
            HeatRecoveryBypassOpen = mode is AwgOperatingMode.Fault or AwgOperatingMode.ControlledShutdown,
            AdsorptionBedEnabled = mode is AwgOperatingMode.Adsorption or AwgOperatingMode.Regeneration,
            RegenerationHeatEnabled = mode == AwgOperatingMode.Regeneration,
            CondenserEnabled = condenserEnabled,
            ReasonCode = reasonCode,
            ActiveFaultCode = faultCode
        };
    }

    private static Dictionary<string, double> CaptureScalars(
        AwgSystemObservation observation,
        AwgControlParameters parameters)
        => new(StringComparer.Ordinal)
        {
            ["batterySoc"] = observation.BatteryStateOfChargeFraction,
            ["silicaLoading"] = observation.SilicaGelLoadingKgPerKg,
            ["silicaEquilibriumLoading"] = observation.SilicaGelEquilibriumLoadingKgPerKg,
            ["solarIrradiance"] = observation.SolarIrradianceWPerSquareMeter,
            ["availablePowerW"] = observation.AvailableElectricalPowerW,
            ["airflow"] = observation.ProcessDryAirMassFlowKgPerSecond,
            ["tankLevel"] = observation.WaterTankLevelFraction,
            ["regenEntryLoading"] = parameters.RegenerationEntryLoadingKgPerKg,
            ["regenExitLoading"] = parameters.RegenerationExitLoadingKgPerKg,
            ["adsorptionTarget"] = parameters.AdsorptionTargetLoadingKgPerKg
        };

    private static SimulationDiagnostic Error(string code, string message)
        => new() { Code = code, Severity = DiagnosticSeverity.Error, Message = message };

    private static SimulationDiagnostic Warning(string code, string message)
        => new() { Code = code, Severity = DiagnosticSeverity.Warning, Message = message };

    private static SimulationDiagnostic Info(string code, string message)
        => new() { Code = code, Severity = DiagnosticSeverity.Information, Message = message };

    private readonly record struct ModeDecision(
        AwgOperatingMode Mode,
        string ReasonCode,
        string Constraint,
        bool BypassDwell = false);
}
