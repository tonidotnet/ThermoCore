using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// Fidelity Level 3 analytical thermoelectric cooler
/// (docs/03_Components/08_Peltier.md §9–§14, §57 / TEC-002).
/// Steady-state α, R, K model with power-request current solver and off-state conduction.
/// </summary>
public sealed class AnalyticalPeltierComponent : ISimulationComponent
{
    private readonly AnalyticalPeltierParameters _parameters;
    private readonly double _fallbackColdSideTemperatureK;
    private readonly double _fallbackHotSideTemperatureK;
    private readonly double _requestedElectricalPowerW;
    private readonly double? _requestedCurrentA;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public AnalyticalPeltierComponent(
        string id,
        AnalyticalPeltierParameters parameters,
        double coldSideTemperatureK,
        double hotSideTemperatureK,
        double requestedElectricalPowerW = 0.0,
        double? requestedCurrentA = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(parameters);
        FiniteNumber.RequirePositive(coldSideTemperatureK, nameof(coldSideTemperatureK));
        FiniteNumber.RequirePositive(hotSideTemperatureK, nameof(hotSideTemperatureK));
        FiniteNumber.RequireNonNegative(requestedElectricalPowerW, nameof(requestedElectricalPowerW));
        if (requestedCurrentA is { } current)
        {
            FiniteNumber.Require(current, nameof(requestedCurrentA));
        }

        Id = id;
        _parameters = parameters.Validate();
        _fallbackColdSideTemperatureK = coldSideTemperatureK;
        _fallbackHotSideTemperatureK = hotSideTemperatureK;
        _requestedElectricalPowerW = requestedElectricalPowerW;
        _requestedCurrentA = requestedCurrentA;

        Ports =
        [
            new PhysicalPort("cold_heat", id, PortDirection.Output, PhysicalDomain.Heat),
            new PhysicalPort("hot_heat", id, PortDirection.Output, PhysicalDomain.Heat),
            new PhysicalPort("electrical", id, PortDirection.Input, PhysicalDomain.Electricity, isRequired: false),
            new PhysicalPort("cold_boundary", id, PortDirection.Input, PhysicalDomain.Heat, isRequired: false),
            new PhysicalPort("hot_boundary", id, PortDirection.Input, PhysicalDomain.Heat, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastColdSideHeatW { get; private set; }

    public double LastHotSideHeatW { get; private set; }

    public double LastElectricalPowerW { get; private set; }

    public double LastCurrentA { get; private set; }

    public double LastVoltageV { get; private set; }

    public double LastCoolingCop { get; private set; }

    public double LastLoadTemperatureK { get; private set; }

    public double LastSinkTemperatureK { get; private set; }

    public double LastColdFaceTemperatureK { get; private set; }

    public double LastHotFaceTemperatureK { get; private set; }

    public bool LastProtectionTripped { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastColdSideHeatW = 0.0;
        LastHotSideHeatW = 0.0;
        LastElectricalPowerW = 0.0;
        LastCurrentA = 0.0;
        LastVoltageV = 0.0;
        LastCoolingCop = 0.0;
        LastLoadTemperatureK = 0.0;
        LastSinkTemperatureK = 0.0;
        LastColdFaceTemperatureK = 0.0;
        LastHotFaceTemperatureK = 0.0;
        LastProtectionTripped = false;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();

        var loadTemperatureK = _fallbackColdSideTemperatureK;
        var sinkTemperatureK = _fallbackHotSideTemperatureK;

        if (context.InputStates.TryGetValue("cold_boundary", out var coldRaw)
            && coldRaw is HeatFlowState coldBoundary)
        {
            FiniteNumber.RequirePositive(coldBoundary.TemperatureK, nameof(coldBoundary.TemperatureK));
            loadTemperatureK = coldBoundary.TemperatureK;
        }

        if (context.InputStates.TryGetValue("hot_boundary", out var hotRaw)
            && hotRaw is HeatFlowState hotBoundary)
        {
            FiniteNumber.RequirePositive(hotBoundary.TemperatureK, nameof(hotBoundary.TemperatureK));
            sinkTemperatureK = hotBoundary.TemperatureK;
        }

        var rCold = _parameters.ColdSideThermalResistanceKPerW;
        var rHot = _parameters.HotSideThermalResistanceKPerW;
        var useExternalResistances = rCold > 0.0 || rHot > 0.0;

        var coldFaceTemperatureK = loadTemperatureK;
        var hotFaceTemperatureK = sinkTemperatureK;
        var alpha = _parameters.SeebeckCoefficientVPerK;
        var resistance = _parameters.ElectricalResistanceOhm;
        var conductance = _parameters.ThermalConductanceWPerK;
        var tolerances = context.Simulation.NumericalTolerances;
        var maxIterations = useExternalResistances
            ? Math.Max(8, Math.Min(tolerances.MaximumIterations, 40))
            : 1;

        double currentA = 0.0;
        double voltageV = 0.0;
        double electricalPowerW = 0.0;
        double coldSideHeatW = 0.0;
        double hotSideHeatW = 0.0;
        var limitedByCurrent = false;
        var limitedByVoltage = false;
        var limitedByPower = false;
        var thermalNotConverged = false;

        var requestedPowerW = _requestedElectricalPowerW;
        if (_requestedCurrentA is null
            && context.InputStates.TryGetValue("electrical", out var electricalRaw)
            && electricalRaw is ElectricalPowerState electrical)
        {
            FiniteNumber.RequireNonNegative(electrical.PowerW, nameof(electrical.PowerW));
            requestedPowerW = electrical.PowerW;
        }

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var deltaTemperatureK = hotFaceTemperatureK - coldFaceTemperatureK;

            SolveElectricalOperatingPoint(
                alpha,
                resistance,
                deltaTemperatureK,
                requestedPowerW,
                out currentA,
                out voltageV,
                out electricalPowerW,
                out var iterCurrentLimit,
                out var iterVoltageLimit,
                out var iterPowerLimit);
            limitedByCurrent = iterCurrentLimit;
            limitedByVoltage = iterVoltageLimit;
            limitedByPower = iterPowerLimit;

            coldSideHeatW = alpha * currentA * coldFaceTemperatureK
                - 0.5 * currentA * currentA * resistance
                - conductance * deltaTemperatureK;
            hotSideHeatW = coldSideHeatW + electricalPowerW;

            if (!useExternalResistances)
            {
                break;
            }

            var nextColdFace = rCold > 0.0
                ? loadTemperatureK - coldSideHeatW * rCold
                : loadTemperatureK;
            var nextHotFace = rHot > 0.0
                ? sinkTemperatureK + hotSideHeatW * rHot
                : sinkTemperatureK;

            nextColdFace = Math.Clamp(nextColdFace, 200.0, 400.0);
            nextHotFace = Math.Clamp(nextHotFace, 200.0, 450.0);

            var coldResidual = Math.Abs(nextColdFace - coldFaceTemperatureK);
            var hotResidual = Math.Abs(nextHotFace - hotFaceTemperatureK);
            coldFaceTemperatureK = 0.5 * (coldFaceTemperatureK + nextColdFace);
            hotFaceTemperatureK = 0.5 * (hotFaceTemperatureK + nextHotFace);

            if (coldResidual < tolerances.TemperatureK && hotResidual < tolerances.TemperatureK)
            {
                coldFaceTemperatureK = nextColdFace;
                hotFaceTemperatureK = nextHotFace;
                thermalNotConverged = false;
                break;
            }

            thermalNotConverged = iteration == maxIterations - 1;
        }

        if (thermalNotConverged)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.THERMAL_RESISTANCE_NOT_CONVERGED",
                DiagnosticSeverity.Warning,
                "External thermal-resistance fixed-point did not fully converge."));
        }

        if (coldFaceTemperatureK < _parameters.MinimumColdSideTemperatureK)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.COLD_SIDE_TEMPERATURE_LIMIT",
                DiagnosticSeverity.Warning,
                $"Cold-face temperature {coldFaceTemperatureK:F2} K is below configured minimum."));
        }

        if (hotFaceTemperatureK > _parameters.MaximumHotSideTemperatureK)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.HOT_SIDE_TEMPERATURE_LIMIT",
                DiagnosticSeverity.Warning,
                $"Hot-face temperature {hotFaceTemperatureK:F2} K exceeds configured maximum."));
        }

        var faceDeltaT = hotFaceTemperatureK - coldFaceTemperatureK;
        if (_parameters.MaximumTemperatureDifferenceK is { } maxDelta && faceDeltaT > maxDelta)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.DELTA_T_LIMIT",
                DiagnosticSeverity.Warning,
                $"Hot-cold face ΔT {faceDeltaT:F2} K exceeds configured maximum {maxDelta:F2} K."));
        }

        if (limitedByCurrent)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.CURRENT_LIMIT",
                DiagnosticSeverity.Information,
                "Operating current was limited to the configured maximum."));
        }

        if (limitedByVoltage)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.VOLTAGE_LIMIT",
                DiagnosticSeverity.Information,
                "Operating voltage was limited to the configured maximum."));
        }

        if (limitedByPower)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.POWER_LIMIT",
                DiagnosticSeverity.Information,
                "Electrical power was limited to the configured maximum."));
        }

        if (currentA == 0.0 && Math.Abs(faceDeltaT) > 1e-12)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.OFF_STATE_CONDUCTION",
                DiagnosticSeverity.Information,
                "Module is electrically off; passive conduction remains active."));
        }

        if (coldSideHeatW < 0.0 && currentA != 0.0)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.NO_NET_COOLING",
                DiagnosticSeverity.Warning,
                "Calculated cold-side heat flow is negative; the operating point provides no net cooling."));
        }

        var coolingCop = electricalPowerW > 1e-12 && coldSideHeatW > 0.0
            ? coldSideHeatW / electricalPowerW
            : 0.0;

        // TEC-007 safety diagnostics and optional protection shutdown.
        var protectionTrip = false;
        if (hotFaceTemperatureK > _parameters.MaximumHotSideTemperatureK)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.HOT_SIDE_OVERTEMPERATURE",
                DiagnosticSeverity.Warning,
                "Hot-face temperature exceeds the configured maximum."));
            protectionTrip = _parameters.EnableProtectionShutdown;
        }

        if (coldFaceTemperatureK < _parameters.MinimumColdSideTemperatureK)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.COLD_SIDE_UNDERTEMPERATURE",
                DiagnosticSeverity.Warning,
                "Cold-face temperature is below the configured minimum."));
            protectionTrip = protectionTrip || _parameters.EnableProtectionShutdown;
        }

        if (_parameters.MinimumUsefulCoolingCop > 0.0
            && electricalPowerW > 1e-12
            && coolingCop < _parameters.MinimumUsefulCoolingCop)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.COP_BELOW_USEFUL_THRESHOLD",
                DiagnosticSeverity.Warning,
                $"Cooling COP {coolingCop:F3} is below useful threshold {_parameters.MinimumUsefulCoolingCop:F3}."));
        }

        if (_parameters.MaximumAllowedColdSideHeatFluxWPerM2 > 0.0
            && _parameters.ActiveColdSideAreaM2 > 0.0
            && coldSideHeatW > 0.0)
        {
            var heatFlux = coldSideHeatW / _parameters.ActiveColdSideAreaM2;
            if (heatFlux > _parameters.MaximumAllowedColdSideHeatFluxWPerM2)
            {
                diagnostics.Add(Diagnostic(
                    context,
                    "PELTIER.COLD_SIDE_HEAT_FLUX_LIMIT",
                    DiagnosticSeverity.Warning,
                    "Cold-side heat flux exceeds the configured maximum."));
            }
        }

        if (_parameters.HotSideThermalResistanceKPerW > _parameters.HotSideThermalResistanceWarningKPerW)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.HOT_SIDE_RESISTANCE_HIGH",
                DiagnosticSeverity.Information,
                "Configured hot-side thermal resistance is above the warning threshold."));
        }

        if (_parameters.ColdSideThermalResistanceKPerW > _parameters.ColdSideThermalResistanceWarningKPerW)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.COLD_SIDE_RESISTANCE_HIGH",
                DiagnosticSeverity.Information,
                "Configured cold-side thermal resistance is above the warning threshold."));
        }

        if (protectionTrip)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.MODULE_DISABLED_BY_PROTECTION",
                DiagnosticSeverity.Critical,
                "Module electrical drive was disabled by thermal protection."));
            requestedPowerW = 0.0;
            // Re-evaluate passive conduction-only operating point.
            var deltaOff = hotFaceTemperatureK - coldFaceTemperatureK;
            currentA = 0.0;
            voltageV = alpha * deltaOff;
            electricalPowerW = 0.0;
            coldSideHeatW = -conductance * deltaOff;
            hotSideHeatW = coldSideHeatW;
            coolingCop = 0.0;
            LastProtectionTripped = true;
        }
        else
        {
            LastProtectionTripped = false;
        }

        LastColdSideHeatW = coldSideHeatW;
        LastHotSideHeatW = hotSideHeatW;
        LastElectricalPowerW = electricalPowerW;
        LastCurrentA = currentA;
        LastVoltageV = voltageV;
        LastCoolingCop = coolingCop;
        LastLoadTemperatureK = loadTemperatureK;
        LastSinkTemperatureK = sinkTemperatureK;
        LastColdFaceTemperatureK = coldFaceTemperatureK;
        LastHotFaceTemperatureK = hotFaceTemperatureK;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.0,
            dryAirMassOutputKgPerSecond: 0.0,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: 0.0,
            waterMassOutputKgPerSecond: 0.0,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: electricalPowerW + coldSideHeatW,
            energyOutputW: hotSideHeatW,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep,
            electricalPowerInputW: Math.Max(0.0, electricalPowerW),
            electricalPowerOutputW: Math.Max(0.0, electricalPowerW));

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cold_heat"] = new HeatFlowState
                {
                    HeatFlowW = coldSideHeatW,
                    TemperatureK = coldFaceTemperatureK
                },
                ["hot_heat"] = new HeatFlowState
                {
                    HeatFlowW = hotSideHeatW,
                    TemperatureK = hotFaceTemperatureK
                }
            },
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    private void SolveElectricalOperatingPoint(
        double alpha,
        double resistance,
        double deltaTemperatureK,
        double requestedPowerW,
        out double currentA,
        out double voltageV,
        out double electricalPowerW,
        out bool limitedByCurrent,
        out bool limitedByVoltage,
        out bool limitedByPower)
    {
        limitedByCurrent = false;
        limitedByVoltage = false;
        limitedByPower = false;

        if (_requestedCurrentA is { } fixedCurrent)
        {
            currentA = fixedCurrent;
        }
        else
        {
            var power = requestedPowerW;
            if (power > _parameters.MaximumElectricalPowerW)
            {
                power = _parameters.MaximumElectricalPowerW;
                limitedByPower = true;
            }

            currentA = SolveCurrentFromElectricalPower(alpha, resistance, deltaTemperatureK, power);
        }

        if (!_parameters.AllowReverseCurrent && currentA < 0.0)
        {
            currentA = 0.0;
        }

        if (Math.Abs(currentA) > _parameters.MaximumCurrentA)
        {
            currentA = Math.Sign(currentA) * _parameters.MaximumCurrentA;
            limitedByCurrent = true;
        }

        voltageV = alpha * deltaTemperatureK + currentA * resistance;
        if (Math.Abs(voltageV) > _parameters.MaximumVoltageV)
        {
            var limitedCurrent = (Math.Sign(voltageV) * _parameters.MaximumVoltageV - alpha * deltaTemperatureK)
                / resistance;
            if (!_parameters.AllowReverseCurrent && limitedCurrent < 0.0)
            {
                limitedCurrent = 0.0;
            }

            currentA = limitedCurrent;
            voltageV = alpha * deltaTemperatureK + currentA * resistance;
            limitedByVoltage = true;
        }

        electricalPowerW = voltageV * currentA;
        if (electricalPowerW > _parameters.MaximumElectricalPowerW)
        {
            electricalPowerW = _parameters.MaximumElectricalPowerW;
            currentA = SolveCurrentFromElectricalPower(alpha, resistance, deltaTemperatureK, electricalPowerW);
            if (Math.Abs(currentA) > _parameters.MaximumCurrentA)
            {
                currentA = Math.Sign(currentA) * _parameters.MaximumCurrentA;
                limitedByCurrent = true;
            }

            voltageV = alpha * deltaTemperatureK + currentA * resistance;
            electricalPowerW = voltageV * currentA;
            limitedByPower = true;
        }
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    /// <summary>
    /// Positive cooling-current root of R I² + α ΔT I − Pe = 0
    /// (docs/03_Components/08_Peltier.md §14).
    /// </summary>
    public static double SolveCurrentFromElectricalPower(
        double seebeckCoefficientVPerK,
        double electricalResistanceOhm,
        double deltaTemperatureK,
        double electricalPowerW)
    {
        FiniteNumber.RequirePositive(seebeckCoefficientVPerK, nameof(seebeckCoefficientVPerK));
        FiniteNumber.RequirePositive(electricalResistanceOhm, nameof(electricalResistanceOhm));
        FiniteNumber.Require(deltaTemperatureK, nameof(deltaTemperatureK));
        FiniteNumber.RequireNonNegative(electricalPowerW, nameof(electricalPowerW));

        if (electricalPowerW == 0.0)
        {
            return 0.0;
        }

        var alphaDelta = seebeckCoefficientVPerK * deltaTemperatureK;
        var discriminant = alphaDelta * alphaDelta
            + 4.0 * electricalResistanceOhm * electricalPowerW;
        if (discriminant < 0.0)
        {
            throw new InvalidOperationException(
                "Electrical power / thermoelectric discriminant is negative; no real current exists.");
        }

        return (-alphaDelta + Math.Sqrt(discriminant)) / (2.0 * electricalResistanceOhm);
    }

    private SimulationDiagnostic Diagnostic(
        ComponentStepContext context,
        string code,
        DiagnosticSeverity severity,
        string message)
        => new()
        {
            Code = code,
            Severity = severity,
            Message = message,
            ComponentId = Id,
            StepIndex = context.Simulation.StepIndex,
            SimulationTime = context.Simulation.ElapsedTime
        };
}
