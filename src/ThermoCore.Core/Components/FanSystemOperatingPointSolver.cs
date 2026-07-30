using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Finds fan/system curve intersection Δp_fan(V̇)=Δp_sys(V̇) (AIR-005/AIR-006).
/// </summary>
public static class FanSystemOperatingPointSolver
{
    public static bool TrySolve(
        Func<double, double> fanPressureRisePa,
        Func<double, double> systemPressureDropPa,
        double volumetricFlowGuessM3PerSecond,
        out double volumetricFlowM3PerSecond,
        out double pressurePa,
        double maxVolumetricFlowM3PerSecond = 5.0,
        int maxIterations = 80)
    {
        ArgumentNullException.ThrowIfNull(fanPressureRisePa);
        ArgumentNullException.ThrowIfNull(systemPressureDropPa);
        FiniteNumber.RequirePositive(volumetricFlowGuessM3PerSecond, nameof(volumetricFlowGuessM3PerSecond));
        FiniteNumber.RequirePositive(maxVolumetricFlowM3PerSecond, nameof(maxVolumetricFlowM3PerSecond));

        double Residual(double v) => fanPressureRisePa(v) - systemPressureDropPa(v);

        var lo = 0.0;
        var hi = maxVolumetricFlowM3PerSecond;
        var rLo = Residual(lo);
        var rHi = Residual(hi);
        if (rLo == 0.0)
        {
            volumetricFlowM3PerSecond = lo;
            pressurePa = systemPressureDropPa(lo);
            return true;
        }

        if (rLo * rHi > 0.0)
        {
            // Expand hi a few times from the guess.
            hi = Math.Max(volumetricFlowGuessM3PerSecond, 1e-3);
            rHi = Residual(hi);
            for (var i = 0; i < 20 && rLo * rHi > 0.0; i++)
            {
                hi *= 1.5;
                if (hi > maxVolumetricFlowM3PerSecond * 4.0)
                {
                    break;
                }

                rHi = Residual(hi);
            }

            if (rLo * rHi > 0.0)
            {
                volumetricFlowM3PerSecond = 0.0;
                pressurePa = 0.0;
                return false;
            }
        }

        for (var i = 0; i < maxIterations; i++)
        {
            var mid = 0.5 * (lo + hi);
            var rMid = Residual(mid);
            if (Math.Abs(rMid) < 1e-4 || Math.Abs(hi - lo) < 1e-8)
            {
                volumetricFlowM3PerSecond = mid;
                pressurePa = systemPressureDropPa(mid);
                return pressurePa >= 0.0 && fanPressureRisePa(mid) >= 0.0;
            }

            if (rLo * rMid <= 0.0)
            {
                hi = mid;
                rHi = rMid;
            }
            else
            {
                lo = mid;
                rLo = rMid;
            }
        }

        volumetricFlowM3PerSecond = 0.5 * (lo + hi);
        pressurePa = systemPressureDropPa(volumetricFlowM3PerSecond);
        return true;
    }
}
