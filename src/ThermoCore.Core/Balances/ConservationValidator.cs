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

public sealed record BalanceValidationResult
{
    public required bool IsValid { get; init; }

    public required double RelativeDryAirMassResidual { get; init; }

    public required double RelativeWaterMassResidual { get; init; }

    public required double RelativeEnergyResidual { get; init; }

    public required double RelativeElectricalEnergyResidual { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}

public interface IConservationValidator
{
    BalanceValidationResult Validate(ConservationBalance balance, BalanceTolerance? tolerance = null);
}

public sealed class ConservationValidator : IConservationValidator
{
    public BalanceValidationResult Validate(ConservationBalance balance, BalanceTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(balance);
        var t = tolerance ?? BalanceTolerance.Default;
        FiniteNumber.RequirePositive(t.AbsoluteDryAirMassKg, nameof(t.AbsoluteDryAirMassKg));
        FiniteNumber.RequirePositive(t.AbsoluteWaterMassKg, nameof(t.AbsoluteWaterMassKg));
        FiniteNumber.RequirePositive(t.AbsoluteEnergyJ, nameof(t.AbsoluteEnergyJ));
        FiniteNumber.RequirePositive(t.AbsoluteElectricalEnergyJ, nameof(t.AbsoluteElectricalEnergyJ));
        FiniteNumber.RequirePositive(t.Relative, nameof(t.Relative));
        FiniteNumber.RequirePositive(t.MinimumScale, nameof(t.MinimumScale));

        var relativeDryAir = RelativeResidual(
            balance.DryAirMassResidualKg,
            balance.DryAirMassInputKg,
            balance.DryAirMassOutputKg,
            balance.DryAirMassStorageChangeKg,
            t.MinimumScale);

        var relativeWater = RelativeResidual(
            balance.WaterMassResidualKg,
            balance.WaterMassInputKg,
            balance.WaterMassOutputKg,
            balance.WaterMassStorageChangeKg,
            t.MinimumScale);

        var relativeEnergy = RelativeResidual(
            balance.EnergyResidualJ,
            balance.EnergyInputJ,
            balance.EnergyOutputJ,
            balance.StoredEnergyChangeJ,
            t.MinimumScale);

        var relativeElectrical = RelativeResidual(
            balance.ElectricalEnergyResidualJ,
            balance.ElectricalEnergyInputJ,
            balance.ElectricalEnergyOutputJ,
            balance.StoredElectricalEnergyChangeJ,
            t.MinimumScale);

        var diagnostics = new List<SimulationDiagnostic>();

        CheckQuantity(
            diagnostics,
            code: "BALANCE.DRY_AIR",
            residual: balance.DryAirMassResidualKg,
            relativeResidual: relativeDryAir,
            absoluteTolerance: t.AbsoluteDryAirMassKg,
            relativeTolerance: t.Relative,
            message: "Dry-air mass balance residual exceeds tolerance.");

        CheckQuantity(
            diagnostics,
            code: "BALANCE.WATER",
            residual: balance.WaterMassResidualKg,
            relativeResidual: relativeWater,
            absoluteTolerance: t.AbsoluteWaterMassKg,
            relativeTolerance: t.Relative,
            message: "Water mass balance residual exceeds tolerance.");

        CheckQuantity(
            diagnostics,
            code: "BALANCE.ENERGY",
            residual: balance.EnergyResidualJ,
            relativeResidual: relativeEnergy,
            absoluteTolerance: t.AbsoluteEnergyJ,
            relativeTolerance: t.Relative,
            message: "Energy balance residual exceeds tolerance.");

        CheckQuantity(
            diagnostics,
            code: "BALANCE.ELECTRICAL",
            residual: balance.ElectricalEnergyResidualJ,
            relativeResidual: relativeElectrical,
            absoluteTolerance: t.AbsoluteElectricalEnergyJ,
            relativeTolerance: t.Relative,
            message: "Electrical energy balance residual exceeds tolerance.");

        return new BalanceValidationResult
        {
            IsValid = diagnostics.Count == 0,
            RelativeDryAirMassResidual = relativeDryAir,
            RelativeWaterMassResidual = relativeWater,
            RelativeEnergyResidual = relativeEnergy,
            RelativeElectricalEnergyResidual = relativeElectrical,
            Diagnostics = diagnostics
        };
    }

    private static double RelativeResidual(
        double residual,
        double input,
        double output,
        double storageChange,
        double minimumScale)
    {
        var scale = Math.Abs(input) + Math.Abs(output) + Math.Abs(storageChange);
        var denominator = Math.Max(scale, minimumScale);
        return Math.Abs(residual) / denominator;
    }

    private static void CheckQuantity(
        List<SimulationDiagnostic> diagnostics,
        string code,
        double residual,
        double relativeResidual,
        double absoluteTolerance,
        double relativeTolerance,
        string message)
    {
        var withinAbsolute = Math.Abs(residual) <= absoluteTolerance;
        var withinRelative = relativeResidual <= relativeTolerance;
        if (withinAbsolute || withinRelative)
        {
            return;
        }

        diagnostics.Add(new SimulationDiagnostic
        {
            Code = code,
            Severity = DiagnosticSeverity.Error,
            Message = message,
            Values = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["residual"] = residual,
                ["relativeResidual"] = relativeResidual,
                ["absoluteTolerance"] = absoluteTolerance,
                ["relativeTolerance"] = relativeTolerance
            }
        });
    }
}
