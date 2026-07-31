using ThermoCore.Core.Graph;

namespace ThermoCore.Core.Simulation;

/// <summary>
/// Optional pre-step callback. Used by AWG supervisory control to observe committed
/// port state and update mutable actuators before the next component evaluation.
/// </summary>
public interface ISimulationStepHook
{
    void BeforeStep(
        SimulationContext context,
        IReadOnlyDictionary<string, object?> committedPortStates);
}
