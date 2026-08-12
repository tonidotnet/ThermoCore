using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>Axis-aligned validity box for the vapor-compression map.</summary>
public sealed record VaporCompressionValidityRange
{
    public required double MinimumEvaporatingTemperatureK { get; init; }

    public required double MaximumEvaporatingTemperatureK { get; init; }

    public required double MinimumCondensingTemperatureK { get; init; }

    public required double MaximumCondensingTemperatureK { get; init; }

    public required double MinimumSpeedFraction { get; init; }

    public required double MaximumSpeedFraction { get; init; }

    public static VaporCompressionValidityRange FromPoints(IReadOnlyList<VaporCompressionMapPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            throw new ArgumentException("At least one map point is required.", nameof(points));
        }

        foreach (var point in points)
        {
            point.Validate();
        }

        return new VaporCompressionValidityRange
        {
            MinimumEvaporatingTemperatureK = points.Min(p => p.EvaporatingTemperatureK),
            MaximumEvaporatingTemperatureK = points.Max(p => p.EvaporatingTemperatureK),
            MinimumCondensingTemperatureK = points.Min(p => p.CondensingTemperatureK),
            MaximumCondensingTemperatureK = points.Max(p => p.CondensingTemperatureK),
            MinimumSpeedFraction = points.Min(p => p.SpeedFraction),
            MaximumSpeedFraction = points.Max(p => p.SpeedFraction)
        }.Validate();
    }

    public VaporCompressionValidityRange Validate()
    {
        FiniteNumber.RequirePositive(MinimumEvaporatingTemperatureK, nameof(MinimumEvaporatingTemperatureK));
        FiniteNumber.RequirePositive(MaximumEvaporatingTemperatureK, nameof(MaximumEvaporatingTemperatureK));
        if (MinimumEvaporatingTemperatureK > MaximumEvaporatingTemperatureK)
        {
            throw new ArgumentException("Evaporating temperature validity range is inverted.");
        }

        FiniteNumber.RequirePositive(MinimumCondensingTemperatureK, nameof(MinimumCondensingTemperatureK));
        FiniteNumber.RequirePositive(MaximumCondensingTemperatureK, nameof(MaximumCondensingTemperatureK));
        if (MinimumCondensingTemperatureK > MaximumCondensingTemperatureK)
        {
            throw new ArgumentException("Condensing temperature validity range is inverted.");
        }

        FiniteNumber.Require(MinimumSpeedFraction, nameof(MinimumSpeedFraction));
        FiniteNumber.Require(MaximumSpeedFraction, nameof(MaximumSpeedFraction));
        if (MinimumSpeedFraction is < 0.0 or > 1.0 || MaximumSpeedFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumSpeedFraction), "Speed fraction bounds must be in [0, 1].");
        }

        if (MinimumSpeedFraction > MaximumSpeedFraction)
        {
            throw new ArgumentException("Speed fraction validity range is inverted.");
        }

        return this;
    }
}

/// <summary>Compressor cycling constraints for plant models (COOL-007) — contract only in R5-001.</summary>
public sealed record VaporCompressionCyclingLimits
{
    public TimeSpan MinimumRuntime { get; init; } = TimeSpan.FromMinutes(3);

    public TimeSpan MinimumOffTime { get; init; } = TimeSpan.FromMinutes(3);

    public VaporCompressionCyclingLimits Validate()
    {
        if (MinimumRuntime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumRuntime));
        }

        if (MinimumOffTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumOffTime));
        }

        return this;
    }
}

/// <summary>Frost / high-side safety thresholds used for map diagnostics.</summary>
public sealed record VaporCompressionSafetyLimits
{
    /// <summary>Warn when evaporating temperature is at or below this value (frost risk).</summary>
    public double? FrostThresholdEvaporatingTemperatureK { get; init; }

    public double? MaximumCondensingTemperatureK { get; init; }

    /// <summary>Optional discharge-gas limit when the caller supplies discharge temperature.</summary>
    public double? MaximumDischargeTemperatureK { get; init; }

    public VaporCompressionSafetyLimits Validate()
    {
        if (FrostThresholdEvaporatingTemperatureK is { } frost)
        {
            FiniteNumber.RequirePositive(frost, nameof(FrostThresholdEvaporatingTemperatureK));
        }

        if (MaximumCondensingTemperatureK is { } tmax)
        {
            FiniteNumber.RequirePositive(tmax, nameof(MaximumCondensingTemperatureK));
        }

        if (MaximumDischargeTemperatureK is { } td)
        {
            FiniteNumber.RequirePositive(td, nameof(MaximumDischargeTemperatureK));
        }

        return this;
    }
}
