using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>Boundary solar irradiance source.</summary>
public sealed class SolarRadiationSourceComponent : ISimulationComponent
{
    private readonly SolarIrradianceState _state;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public SolarRadiationSourceComponent(string id, double irradianceWPerM2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequireNonNegative(irradianceWPerM2, nameof(irradianceWPerM2));
        Id = id;
        _state = new SolarIrradianceState { IrradianceWPerM2 = irradianceWPerM2 };
        Ports =
        [
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.SolarRadiation)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public void Initialize(SimulationContext context) => _diagnostics.Clear();

    public ComponentStepResult Evaluate(ComponentStepContext context)
        => new()
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = _state
            },
            Balance = ConservationBalance.Empty
        };

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}
