using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

public sealed record ResultChannelDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string QuantityType { get; init; }

    public required string Unit { get; init; }

    public required string ComponentId { get; init; }

    public string Description { get; init; } = string.Empty;
}
