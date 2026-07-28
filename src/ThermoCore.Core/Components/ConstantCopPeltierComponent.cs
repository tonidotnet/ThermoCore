using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Fidelity Level 1 constant-COP Peltier model
/// (docs/03_Components/08_Peltier.md §55): Qc = COPc·Pe, Qh = Qc + Pe.
/// </summary>
public sealed class ConstantCopPeltierComponent : ISimulationComponent
{
    private readonly double _coolingCop;
    private readonly double _electricalPowerW;
    private readonly double _coldSideTemperatureK;
    private readonly double _hotSideTemperatureK;
    private readonly double? _maximumDeltaTemperatureK;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public ConstantCopPeltierComponent(
        string id,
        double coolingCop,
        double electricalPowerW,
        double coldSideTemperatureK,
        double hotSideTemperatureK,
        double? maximumDeltaTemperatureK = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequirePositive(coolingCop, nameof(coolingCop));
        FiniteNumber.RequireNonNegative(electricalPowerW, nameof(electricalPowerW));
        FiniteNumber.RequirePositive(coldSideTemperatureK, nameof(coldSideTemperatureK));
        FiniteNumber.RequirePositive(hotSideTemperatureK, nameof(hotSideTemperatureK));

        Id = id;
        _coolingCop = coolingCop;
        _electricalPowerW = electricalPowerW;
        _coldSideTemperatureK = coldSideTemperatureK;
        _hotSideTemperatureK = hotSideTemperatureK;
        _maximumDeltaTemperatureK = maximumDeltaTemperatureK;

        Ports =
        [
            new PhysicalPort("cold_heat", id, PortDirection.Output, PhysicalDomain.Heat),
            new PhysicalPort("hot_heat", id, PortDirection.Output, PhysicalDomain.Heat),
            new PhysicalPort("electrical", id, PortDirection.Input, PhysicalDomain.Electricity, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastColdSideHeatW { get; private set; }

    public double LastHotSideHeatW { get; private set; }

    public double LastElectricalPowerW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastColdSideHeatW = 0.0;
        LastHotSideHeatW = 0.0;
        LastElectricalPowerW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();

        var electricalPowerW = _electricalPowerW;
        if (context.InputStates.TryGetValue("electrical", out var raw)
            && raw is ElectricalPowerState electrical)
        {
            FiniteNumber.RequireNonNegative(electrical.PowerW, nameof(electrical.PowerW));
            electricalPowerW = electrical.PowerW;
        }

        var deltaT = _hotSideTemperatureK - _coldSideTemperatureK;
        if (_maximumDeltaTemperatureK is { } maxDelta && deltaT > maxDelta)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "PELTIER.DELTA_T_LIMIT",
                Severity = DiagnosticSeverity.Warning,
                Message =
                    $"Hot-cold ΔT {deltaT:F2} K exceeds configured maximum {maxDelta:F2} K; cooling set to zero.",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex,
                Values = new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["deltaTemperatureK"] = deltaT,
                    ["maximumDeltaTemperatureK"] = maxDelta
                }
            });
            electricalPowerW = 0.0;
        }

        var coldSideHeatW = _coolingCop * electricalPowerW;
        var hotSideHeatW = coldSideHeatW + electricalPowerW;

        LastColdSideHeatW = coldSideHeatW;
        LastHotSideHeatW = hotSideHeatW;
        LastElectricalPowerW = electricalPowerW;

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
            electricalPowerInputW: electricalPowerW,
            electricalPowerOutputW: electricalPowerW);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cold_heat"] = new HeatFlowState
                {
                    HeatFlowW = coldSideHeatW,
                    TemperatureK = _coldSideTemperatureK
                },
                ["hot_heat"] = new HeatFlowState
                {
                    HeatFlowW = hotSideHeatW,
                    TemperatureK = _hotSideTemperatureK
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
}
