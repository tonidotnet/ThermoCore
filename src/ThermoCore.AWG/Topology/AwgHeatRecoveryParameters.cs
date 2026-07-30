using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>Sensible heat-recovery parameters for the AWG V3 builder.</summary>
public sealed record AwgHeatRecoveryParameters
{
    public double EffectivenessFraction { get; init; } = 0.65;

    public double BypassFraction { get; init; }

    public AwgHeatRecoveryParameters Validate()
    {
        FiniteNumber.Require(EffectivenessFraction, nameof(EffectivenessFraction));
        FiniteNumber.Require(BypassFraction, nameof(BypassFraction));
        if (EffectivenessFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(EffectivenessFraction));
        }

        if (BypassFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(BypassFraction));
        }

        return this;
    }
}
