namespace ThermoCore.Core.Graph;

/// <summary>Heat-transfer rate at a thermal port. Positive values follow component-local conventions documented by each model.</summary>
public sealed record HeatFlowState
{
    public required double HeatFlowW { get; init; }

    public required double TemperatureK { get; init; }
}
