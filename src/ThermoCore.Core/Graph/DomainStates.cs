namespace ThermoCore.Core.Graph;

/// <summary>Heat-transfer rate at a thermal port. Positive values follow component-local conventions documented by each model.</summary>
public sealed record HeatFlowState
{
    public required double HeatFlowW { get; init; }

    public required double TemperatureK { get; init; }
}

/// <summary>Electrical power at a port. Positive means power delivered into the receiving component.</summary>
public sealed record ElectricalPowerState
{
    public required double PowerW { get; init; }
}

/// <summary>Plane-of-array irradiance for solar ports.</summary>
public sealed record SolarIrradianceState
{
    public required double IrradianceWPerM2 { get; init; }
}

/// <summary>Liquid-water stream state.</summary>
public sealed record LiquidWaterState
{
    public required double MassFlowKgPerSecond { get; init; }

    public required double TemperatureK { get; init; }
}
