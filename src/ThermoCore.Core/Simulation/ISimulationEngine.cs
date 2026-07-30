using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Numerics;

namespace ThermoCore.Core.Simulation;

public interface ISimulationEngine
{
    SimulationRunResult Run(
        SimulationRequest request,
        CancellationToken cancellationToken = default,
        IProgress<SimulationProgress>? progress = null);
}
