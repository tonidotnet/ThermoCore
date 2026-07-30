using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Numerics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Balances;

public sealed record BalanceTolerance
{
    public double AbsoluteDryAirMassKg { get; init; } = 1e-9;

    public double AbsoluteWaterMassKg { get; init; } = 1e-9;

    public double AbsoluteEnergyJ { get; init; } = 1e-5;

    public double AbsoluteElectricalEnergyJ { get; init; } = 1e-5;

    public double Relative { get; init; } = 1e-7;

    public double MinimumScale { get; init; } = 1e-12;

    public static BalanceTolerance Default { get; } = new();

    public static BalanceTolerance FromNumericalTolerances(NumericalTolerances tolerances)
    {
        ArgumentNullException.ThrowIfNull(tolerances);
        tolerances.Validate();

        return new BalanceTolerance
        {
            AbsoluteDryAirMassKg = tolerances.MassKg,
            AbsoluteWaterMassKg = tolerances.MassKg,
            AbsoluteEnergyJ = tolerances.EnergyJ,
            AbsoluteElectricalEnergyJ = tolerances.EnergyJ,
            Relative = tolerances.Relative,
            MinimumScale = 1e-12
        };
    }
}
