using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Topology;

/// <summary>Prescribed-flow fan parameters for the process airflow driver.</summary>
public sealed record AwgFanParameters
{
    public required double DryAirMassFlowKgPerSecond { get; init; }

    public required double PressureRisePa { get; init; }

    public double FanEfficiency { get; init; } = 0.60;

    public double DriverEfficiency { get; init; } = 0.90;

    public AwgFanParameters Validate()
    {
        FiniteNumber.RequirePositive(DryAirMassFlowKgPerSecond, nameof(DryAirMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(PressureRisePa, nameof(PressureRisePa));
        FiniteNumber.RequirePositive(FanEfficiency, nameof(FanEfficiency));
        FiniteNumber.RequirePositive(DriverEfficiency, nameof(DriverEfficiency));
        if (FanEfficiency > 1.0 || DriverEfficiency > 1.0)
        {
            throw new ArgumentOutOfRangeException("Fan and driver efficiencies must be in (0, 1].");
        }

        return this;
    }
}
