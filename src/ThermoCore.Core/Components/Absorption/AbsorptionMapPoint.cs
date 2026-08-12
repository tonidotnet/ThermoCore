using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Absorption;

/// <summary>One absorption performance-map point (DOC-037 / R7-001).</summary>
public sealed record AbsorptionMapPoint
{
    public required double GeneratorTemperatureK { get; init; }

    public required double HeatSinkTemperatureK { get; init; }

    public required double EvaporatorTemperatureK { get; init; }

    public required double ThermalInputW { get; init; }

    public required double CoolingOutputW { get; init; }

    /// <summary>Optional stored thermal COP; when null, derived as Qc / Qgen.</summary>
    public double? ThermalCop { get; init; }

    public double EffectiveThermalCop
        => ThermalCop ?? (ThermalInputW > 0.0 ? CoolingOutputW / ThermalInputW : 0.0);

    public AbsorptionMapPoint Validate()
    {
        FiniteNumber.RequirePositive(GeneratorTemperatureK, nameof(GeneratorTemperatureK));
        FiniteNumber.RequirePositive(HeatSinkTemperatureK, nameof(HeatSinkTemperatureK));
        FiniteNumber.RequirePositive(EvaporatorTemperatureK, nameof(EvaporatorTemperatureK));
        if (EvaporatorTemperatureK >= HeatSinkTemperatureK)
        {
            throw new ArgumentException(
                "Evaporator temperature must be below heat-sink temperature.",
                nameof(EvaporatorTemperatureK));
        }

        if (GeneratorTemperatureK <= HeatSinkTemperatureK)
        {
            throw new ArgumentException(
                "Generator temperature must exceed heat-sink temperature for a driven absorption cycle.",
                nameof(GeneratorTemperatureK));
        }

        FiniteNumber.RequireNonNegative(ThermalInputW, nameof(ThermalInputW));
        FiniteNumber.RequireNonNegative(CoolingOutputW, nameof(CoolingOutputW));

        if (ThermalCop is { } cop)
        {
            FiniteNumber.RequireNonNegative(cop, nameof(ThermalCop));
            if (ThermalInputW > 1e-12)
            {
                var derived = CoolingOutputW / ThermalInputW;
                if (Math.Abs(derived - cop) > Math.Max(1e-6, 0.05 * Math.Max(derived, cop)))
                {
                    throw new ArgumentException(
                        $"Stored thermal COP {cop} disagrees with CoolingOutputW/ThermalInputW ({derived}).",
                        nameof(ThermalCop));
                }
            }
        }

        return this;
    }
}

/// <summary>Axis-aligned validity box for absorption research maps.</summary>
public sealed record AbsorptionValidityRange
{
    public required double MinimumGeneratorTemperatureK { get; init; }

    public required double MaximumGeneratorTemperatureK { get; init; }

    public required double MinimumHeatSinkTemperatureK { get; init; }

    public required double MaximumHeatSinkTemperatureK { get; init; }

    public required double MinimumEvaporatorTemperatureK { get; init; }

    public required double MaximumEvaporatorTemperatureK { get; init; }

    public static AbsorptionValidityRange FromPoints(IReadOnlyList<AbsorptionMapPoint> points)
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

        return new AbsorptionValidityRange
        {
            MinimumGeneratorTemperatureK = points.Min(p => p.GeneratorTemperatureK),
            MaximumGeneratorTemperatureK = points.Max(p => p.GeneratorTemperatureK),
            MinimumHeatSinkTemperatureK = points.Min(p => p.HeatSinkTemperatureK),
            MaximumHeatSinkTemperatureK = points.Max(p => p.HeatSinkTemperatureK),
            MinimumEvaporatorTemperatureK = points.Min(p => p.EvaporatorTemperatureK),
            MaximumEvaporatorTemperatureK = points.Max(p => p.EvaporatorTemperatureK)
        }.Validate();
    }

    public AbsorptionValidityRange Validate()
    {
        FiniteNumber.RequirePositive(MinimumGeneratorTemperatureK, nameof(MinimumGeneratorTemperatureK));
        FiniteNumber.RequirePositive(MaximumGeneratorTemperatureK, nameof(MaximumGeneratorTemperatureK));
        if (MinimumGeneratorTemperatureK > MaximumGeneratorTemperatureK)
        {
            throw new ArgumentException("Generator temperature validity range is inverted.");
        }

        FiniteNumber.RequirePositive(MinimumHeatSinkTemperatureK, nameof(MinimumHeatSinkTemperatureK));
        FiniteNumber.RequirePositive(MaximumHeatSinkTemperatureK, nameof(MaximumHeatSinkTemperatureK));
        if (MinimumHeatSinkTemperatureK > MaximumHeatSinkTemperatureK)
        {
            throw new ArgumentException("Heat-sink temperature validity range is inverted.");
        }

        FiniteNumber.RequirePositive(MinimumEvaporatorTemperatureK, nameof(MinimumEvaporatorTemperatureK));
        FiniteNumber.RequirePositive(MaximumEvaporatorTemperatureK, nameof(MaximumEvaporatorTemperatureK));
        if (MinimumEvaporatorTemperatureK > MaximumEvaporatorTemperatureK)
        {
            throw new ArgumentException("Evaporator temperature validity range is inverted.");
        }

        return this;
    }
}
