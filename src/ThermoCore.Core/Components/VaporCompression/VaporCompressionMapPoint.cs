using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>One manufacturer / lab performance-map operating point (COOL-006).</summary>
public sealed record VaporCompressionMapPoint
{
    public required double EvaporatingTemperatureK { get; init; }

    public required double CondensingTemperatureK { get; init; }

    /// <summary>Normalized compressor speed / capacity control in [0, 1].</summary>
    public required double SpeedFraction { get; init; }

    public required double CoolingCapacityW { get; init; }

    public required double ElectricalPowerW { get; init; }

    /// <summary>Optional stored COP; when null, derived as Qc/Pe.</summary>
    public double? Cop { get; init; }

    public double EffectiveCop
        => Cop ?? (ElectricalPowerW > 0.0 ? CoolingCapacityW / ElectricalPowerW : 0.0);

    public VaporCompressionMapPoint Validate()
    {
        FiniteNumber.RequirePositive(EvaporatingTemperatureK, nameof(EvaporatingTemperatureK));
        FiniteNumber.RequirePositive(CondensingTemperatureK, nameof(CondensingTemperatureK));
        if (EvaporatingTemperatureK >= CondensingTemperatureK)
        {
            throw new ArgumentException(
                "Evaporating temperature must be below condensing temperature.",
                nameof(EvaporatingTemperatureK));
        }

        FiniteNumber.Require(SpeedFraction, nameof(SpeedFraction));
        if (SpeedFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(SpeedFraction), "Speed fraction must be in [0, 1].");
        }

        FiniteNumber.RequireNonNegative(CoolingCapacityW, nameof(CoolingCapacityW));
        FiniteNumber.RequireNonNegative(ElectricalPowerW, nameof(ElectricalPowerW));

        if (Cop is { } cop)
        {
            FiniteNumber.RequireNonNegative(cop, nameof(Cop));
            if (ElectricalPowerW > 1e-12)
            {
                var derived = CoolingCapacityW / ElectricalPowerW;
                if (Math.Abs(derived - cop) > Math.Max(1e-6, 0.02 * Math.Max(derived, cop)))
                {
                    throw new ArgumentException(
                        $"Stored COP {cop} disagrees with CoolingCapacityW/ElectricalPowerW ({derived}).",
                        nameof(Cop));
                }
            }
        }

        return this;
    }
}
