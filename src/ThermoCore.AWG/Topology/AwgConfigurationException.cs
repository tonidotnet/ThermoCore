using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;

namespace ThermoCore.AWG.Topology;

public sealed class AwgConfigurationException : Exception
{
    public AwgConfigurationException(string message, IReadOnlyList<SimulationDiagnostic>? diagnostics = null)
        : base(message)
    {
        Diagnostics = diagnostics ?? Array.Empty<SimulationDiagnostic>();
    }

    public IReadOnlyList<SimulationDiagnostic> Diagnostics { get; }
}
