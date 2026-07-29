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

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastColdSideHeatW = 0.0;
        LastHotSideHeatW = 0.0;
        LastElectricalPowerW = 0.0;
        LastCurrentA = 0.0;
        LastVoltageV = 0.0;
        LastCoolingCop = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();

        var coldSideTemperatureK = _fallbackColdSideTemperatureK;
        var hotSideTemperatureK = _fallbackHotSideTemperatureK;

        if (context.InputStates.TryGetValue("cold_boundary", out var coldRaw)
            && coldRaw is HeatFlowState coldBoundary)
        {
            FiniteNumber.RequirePositive(coldBoundary.TemperatureK, nameof(coldBoundary.TemperatureK));
            coldSideTemperatureK = coldBoundary.TemperatureK;
        }

        if (context.InputStates.TryGetValue("hot_boundary", out var hotRaw)
            && hotRaw is HeatFlowState hotBoundary)
        {
            FiniteNumber.RequirePositive(hotBoundary.TemperatureK, nameof(hotBoundary.TemperatureK));
            hotSideTemperatureK = hotBoundary.TemperatureK;
        }

        if (coldSideTemperatureK < _parameters.MinimumColdSideTemperatureK)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.COLD_SIDE_TEMPERATURE_LIMIT",
                DiagnosticSeverity.Warning,
                $"Cold-side temperature {coldSideTemperatureK:F2} K is below configured minimum."));
        }

        if (hotSideTemperatureK > _parameters.MaximumHotSideTemperatureK)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.HOT_SIDE_TEMPERATURE_LIMIT",
                DiagnosticSeverity.Warning,
                $"Hot-side temperature {hotSideTemperatureK:F2} K exceeds configured maximum."));
        }

        var deltaTemperatureK = hotSideTemperatureK - coldSideTemperatureK;
        if (_parameters.MaximumTemperatureDifferenceK is { } maxDelta
            && deltaTemperatureK > maxDelta)
        {
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.DELTA_T_LIMIT",
                DiagnosticSeverity.Warning,
                $"Hot-cold ΔT {deltaTemperatureK:F2} K exceeds configured maximum {maxDelta:F2} K."));
        }

        var alpha = _parameters.SeebeckCoefficientVPerK;
        var resistance = _parameters.ElectricalResistanceOhm;
        var conductance = _parameters.ThermalConductanceWPerK;

        double currentA;
        var limitedByCurrent = false;
        var limitedByVoltage = false;
        var limitedByPower = false;

        if (_requestedCurrentA is { } fixedCurrent)
        {
            currentA = fixedCurrent;
        }
        else
        {
            var requestedPowerW = _requestedElectricalPowerW;
            if (context.InputStates.TryGetValue("electrical", out var electricalRaw)
                && electricalRaw is ElectricalPowerState electrical)
            {
                FiniteNumber.RequireNonNegative(electrical.PowerW, nameof(electrical.PowerW));
                requestedPowerW = electrical.PowerW;
            }

            if (requestedPowerW > _parameters.MaximumElectricalPowerW)
            {
                requestedPowerW = _parameters.MaximumElectricalPowerW;
                limitedByPower = true;
            }

            currentA = SolveCurrentFromElectricalPower(alpha, resistance, deltaTemperatureK, requestedPowerW);
        }

        if (!_parameters.AllowReverseCurrent && currentA < 0.0)
        {
            currentA = 0.0;
            diagnostics.Add(Diagnostic(
                context,
                "PELTIER.REVERSE_CURRENT_BLOCKED",
                DiagnosticSeverity.Information,
                "Negative current was clamped to zero because reverse operation is disabled."));
        }

        if (Math.Abs(currentA) > _parameters.MaximumCurrentA)
        {
            currentA = Math.Sign(currentA) * _parameters.MaximumCurrentA;
            limitedByCurrent = true;
        }

        var voltageV = alpha * deltaTemperatureK + currentA * resistance;
        if (Math.Abs(voltageV) > _parameters.MaximumVoltageV)
        {
            // Enforce voltage limit by reducing current magnitude.
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

        var electricalPowerW = voltageV * currentA;
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

        var coldSideHeatW = alpha * currentA * coldSideTemperatureK
            - 0.5 * currentA * currentA * resistance
            - conductance * deltaTemperatureK;

        var hotSideHeatW = alpha * currentA * hotSideTemperatureK
            + 0.5 * currentA * currentA * resistance
            - conductance * deltaTemperatureK;

        // Identity Qh = Qc + Pe should hold analytically; recompute hot side from it for numerical hygiene.
        hotSideHeatW = coldSideHeatW + electricalPowerW;

        if (currentA == 0.0 && Math.Abs(deltaTemperatureK) > 1e-12)
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

        LastColdSideHeatW = coldSideHeatW;
        LastHotSideHeatW = hotSideHeatW;
        LastElectricalPowerW = electricalPowerW;
        LastCurrentA = currentA;
        LastVoltageV = voltageV;
        LastCoolingCop = coolingCop;

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
                    TemperatureK = coldSideTemperatureK
                },
                ["hot_heat"] = new HeatFlowState
                {
                    HeatFlowW = hotSideHeatW,
                    TemperatureK = hotSideTemperatureK
                }
            },
            Balance = balance,
            Diagnostics = diagnostics
        };
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
