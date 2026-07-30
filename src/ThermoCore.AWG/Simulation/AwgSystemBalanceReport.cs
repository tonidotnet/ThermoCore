namespace ThermoCore.AWG.Simulation;

/// <summary>System-level water/energy/dry-air balance verification report (AWG-018/019).</summary>
public sealed record AwgSystemBalanceReport
{
    public required bool Succeeded { get; init; }

    public required bool WaterBalancePassed { get; init; }

    public required bool EnergyBalancePassed { get; init; }

    public required bool DryAirBalancePassed { get; init; }

    public required double MaxAbsWaterResidualKg { get; init; }

    public required double MaxAbsEnergyResidualJ { get; init; }

    public required double MaxAbsDryAirResidualKg { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedDryAirResidualKg { get; init; }

    public required double AbsoluteWaterToleranceKg { get; init; }

    public required double AbsoluteEnergyToleranceJ { get; init; }

    public required double AbsoluteDryAirToleranceKg { get; init; }

    public required int CheckedStepCount { get; init; }

    public bool AllPassed => WaterBalancePassed && EnergyBalancePassed && DryAirBalancePassed;
}
