using ThermoCore.Core.Balances;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>
/// Verifies system water and energy residuals for an AWG run
/// (docs/02_Mathematics/04_MathematicalModel.md system balances; AWG-018/019).
/// Uses the same absolute-or-relative acceptance rule as <see cref="ConservationValidator"/>.
/// </summary>
public static class AwgSystemBalanceVerifier
{
    public static AwgSystemBalanceReport Verify(
        SimulationRunResult engineResult,
        BalanceTolerance? tolerance = null,
        IConservationValidator? validator = null)
    {
        ArgumentNullException.ThrowIfNull(engineResult);
        var t = tolerance ?? BalanceTolerance.Default;
        var conservationValidator = validator ?? new ConservationValidator();

        var maxWater = 0.0;
        var maxEnergy = 0.0;
        var maxDryAir = 0.0;
        var waterPassed = true;
        var energyPassed = true;
        var dryAirPassed = true;

        foreach (var step in engineResult.Steps)
        {
            maxWater = Math.Max(maxWater, Math.Abs(step.SystemBalance.WaterMassResidualKg));
            maxEnergy = Math.Max(maxEnergy, Math.Abs(step.SystemBalance.EnergyResidualJ));
            maxDryAir = Math.Max(maxDryAir, Math.Abs(step.SystemBalance.DryAirMassResidualKg));

            var validation = conservationValidator.Validate(step.SystemBalance, t);
            if (validation.Diagnostics.Any(d => d.Code == "BALANCE.WATER"))
            {
                waterPassed = false;
            }

            if (validation.Diagnostics.Any(d => d.Code == "BALANCE.ENERGY"))
            {
                energyPassed = false;
            }

            if (validation.Diagnostics.Any(d => d.Code == "BALANCE.DRY_AIR"))
            {
                dryAirPassed = false;
            }
        }

        return new AwgSystemBalanceReport
        {
            Succeeded = engineResult.Succeeded,
            WaterBalancePassed = waterPassed,
            EnergyBalancePassed = energyPassed,
            DryAirBalancePassed = dryAirPassed,
            MaxAbsWaterResidualKg = maxWater,
            MaxAbsEnergyResidualJ = maxEnergy,
            MaxAbsDryAirResidualKg = maxDryAir,
            AggregatedWaterResidualKg = engineResult.AggregatedBalance.WaterMassResidualKg,
            AggregatedEnergyResidualJ = engineResult.AggregatedBalance.EnergyResidualJ,
            AggregatedDryAirResidualKg = engineResult.AggregatedBalance.DryAirMassResidualKg,
            AbsoluteWaterToleranceKg = t.AbsoluteWaterMassKg,
            AbsoluteEnergyToleranceJ = t.AbsoluteEnergyJ,
            AbsoluteDryAirToleranceKg = t.AbsoluteDryAirMassKg,
            CheckedStepCount = engineResult.Steps.Count
        };
    }
}
