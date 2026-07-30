namespace ThermoCore.Core.Graph;

/// <summary>Electrical power at a port. Positive means power delivered into the receiving component.</summary>
public sealed record ElectricalPowerState
{
    public required double PowerW { get; init; }
}
