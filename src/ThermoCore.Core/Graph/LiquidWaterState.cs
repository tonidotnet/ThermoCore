namespace ThermoCore.Core.Graph;

/// <summary>Liquid-water stream state.</summary>
public sealed record LiquidWaterState
{
    public required double MassFlowKgPerSecond { get; init; }

    public required double TemperatureK { get; init; }
}
