using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Fidelity Level 1 constant-efficiency PV:
/// P_out = η · G_poa · A (docs/03_Components/07_SolarPanel.md §52).
/// </summary>
public sealed class ConstantEfficiencySolarPanelComponent : ISimulationComponent
{
    private readonly double _efficiency;
    private readonly double _areaM2;
    private readonly double _fallbackIrradianceWPerM2;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public ConstantEfficiencySolarPanelComponent(
        string id,
        double efficiency,
        double areaM2,
        double fallbackIrradianceWPerM2 = 0.0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(efficiency, nameof(efficiency));
        if (efficiency is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiency), "PV efficiency must be in [0, 1].");
        }

        FiniteNumber.RequirePositive(areaM2, nameof(areaM2));
        FiniteNumber.RequireNonNegative(fallbackIrradianceWPerM2, nameof(fallbackIrradianceWPerM2));

        Id = id;
        _efficiency = efficiency;
        _areaM2 = areaM2;
        _fallbackIrradianceWPerM2 = fallbackIrradianceWPerM2;
        Ports =
        [
            new PhysicalPort("solar", id, PortDirection.Input, PhysicalDomain.SolarRadiation, isRequired: false),
            new PhysicalPort("electrical", id, PortDirection.Output, PhysicalDomain.Electricity)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastElectricalPowerW { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastElectricalPowerW = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var irradiance = _fallbackIrradianceWPerM2;
        if (context.InputStates.TryGetValue("solar", out var solarRaw)
            && solarRaw is SolarIrradianceState solar)
        {
            FiniteNumber.RequireNonNegative(solar.IrradianceWPerM2, nameof(solar.IrradianceWPerM2));
            irradiance = solar.IrradianceWPerM2;
        }

        LastElectricalPowerW = _efficiency * irradiance * _areaM2;

        // Level-1 bookkeeping: exported DC power is reported on the electrical port.
        // Electrical residual uses "power entering component" convention, so generation is
        // tracked via total energy terms rather than ElectricalEnergyOutput alone.
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.0,
            dryAirMassOutputKgPerSecond: 0.0,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: 0.0,
            waterMassOutputKgPerSecond: 0.0,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: LastElectricalPowerW,
            energyOutputW: LastElectricalPowerW,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["electrical"] = new ElectricalPowerState { PowerW = LastElectricalPowerW }
            },
            Balance = balance
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
