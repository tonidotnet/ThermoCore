namespace ThermoCore.Core.Components.Absorption;

/// <summary>Out-of-range policy for absorption research maps (COOL-008 / R7-001).</summary>
public enum AbsorptionExtrapolationPolicy
{
    ClampWithDiagnostic = 0,
    Reject = 1
}
