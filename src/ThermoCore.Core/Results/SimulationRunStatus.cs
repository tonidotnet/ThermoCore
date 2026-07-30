using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

public enum SimulationRunStatus
{
    Completed,
    CompletedWithWarnings,
    Cancelled,
    FailedValidation,
    FailedConvergence,
    FailedRuntime
}
