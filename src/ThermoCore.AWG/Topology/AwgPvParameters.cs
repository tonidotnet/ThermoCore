using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>Constant-efficiency PV parameters for the MVP electrical subsystem.</summary>
public sealed record AwgPvParameters
{
    public required double EfficiencyFraction { get; init; }

    public required double AreaM2 { get; init; }

    public AwgPvParameters Validate()
    {
        FiniteNumber.Require(EfficiencyFraction, nameof(EfficiencyFraction));
        if (EfficiencyFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(EfficiencyFraction));
        }

        FiniteNumber.RequirePositive(AreaM2, nameof(AreaM2));
        return this;
    }
}
