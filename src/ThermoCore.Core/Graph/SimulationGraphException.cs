using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Core.Graph;

public sealed class SimulationGraphException : Exception
{
    public SimulationGraphException(string message)
        : base(message)
    {
    }

    public SimulationGraphException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
