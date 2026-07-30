using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

public sealed record ResultTimeSeriesChannel
{
    public required ResultChannelDefinition Definition { get; init; }

    public required IReadOnlyList<double> Values { get; init; }
}
