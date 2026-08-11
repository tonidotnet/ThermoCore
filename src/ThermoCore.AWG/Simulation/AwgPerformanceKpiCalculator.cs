using ThermoCore.AWG.Topology;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>
/// Computes water/energy comparison KPIs (KPI-001…KPI-004) from a completed AWG run.
/// Zero denominators yield <c>null</c> (never NaN). Recovered internal heat is never
/// counted as solar primary input.
/// </summary>
public static class AwgPerformanceKpiCalculator
{
    public const double JoulesPerKilowattHour = 3_600_000.0;

    /// <summary>
    /// Peltier electrical proxy uses condenser-cooling heat request (current topology does not
    /// route the cooling actuator onto the DC bus; treat as COP≈1 until cooling-plant accounting).
    /// </summary>
    public static AwgPerformanceKpis Compute(
        AwgBuiltSystem built,
        AwgSimulationOptions options,
        SimulationRunResult engineResult,
        double? collectedWaterKg)
    {
        ArgumentNullException.ThrowIfNull(built);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(engineResult);

        var apertureM2 = built.Configuration.SolarCollector.ApertureAreaM2;
        var waterKg = Math.Max(0.0, collectedWaterKg ?? 0.0);
        var liters = waterKg; // ρ ≈ 1 kg/L for MVP liquid-water reporting
        var dt = options.TimeStep.TotalSeconds;

        double? litersPerDay = null;
        if (options.Duration > TimeSpan.Zero)
        {
            litersPerDay = liters * (TimeSpan.FromDays(1).TotalSeconds / options.Duration.TotalSeconds);
        }

        if (engineResult.Steps.Count == 0 || dt <= 0.0)
        {
            return new AwgPerformanceKpis
            {
                LitersPerDay = litersPerDay,
                SolarCollectorApertureAreaM2 = apertureM2 > 0.0 ? apertureM2 : null,
                LitersPerDayPerSquareMeterAperture = RatioOrNull(
                    litersPerDay,
                    apertureM2 > 0.0 ? apertureM2 : null)
            };
        }

        var busKey = $"{AwgV3TopologyIds.PowerManager}.bus";
        var coolingKey = $"{AwgV3TopologyIds.CondenserCooling}.outlet";
        var solarKey = $"{AwgV3TopologyIds.SolarRadiation}.outlet";
        var ambientKey = $"{AwgV3TopologyIds.AmbientSource}.outlet";
        var collectorOutletKey = $"{AwgV3TopologyIds.SolarCollector}.outlet";
        var bedOutletKey = $"{AwgV3TopologyIds.SilicaGelBed}.outlet";

        var electricJ = 0.0;
        var busJ = 0.0;
        var peltierProxyJ = 0.0;
        var incidentSolarJ = 0.0;
        var ambientMoistureKg = 0.0;
        var desorbedKg = 0.0;
        var sawBus = false;
        var sawSolar = false;
        var sawAmbient = false;
        var sawBedPair = false;

        foreach (var step in engineResult.Steps)
        {
            var busW = TryElectricalW(step.PortStates, busKey);
            if (step.PortStates.ContainsKey(busKey))
            {
                sawBus = true;
            }

            var peltierW = TryHeatW(step.PortStates, coolingKey);
            busJ += busW * dt;
            peltierProxyJ += peltierW * dt;
            electricJ += (busW + peltierW) * dt;

            if (apertureM2 > 0.0
                && step.PortStates.TryGetValue(solarKey, out var solarRaw)
                && solarRaw is SolarIrradianceState solar)
            {
                sawSolar = true;
                incidentSolarJ += Math.Max(0.0, solar.IrradianceWPerM2) * apertureM2 * dt;
            }

            if (step.PortStates.TryGetValue(ambientKey, out var ambientRaw)
                && ambientRaw is MoistAirState ambient)
            {
                sawAmbient = true;
                ambientMoistureKg += Math.Max(0.0, ambient.WaterVaporMassFlowKgPerSecond) * dt;
            }

            if (step.PortStates.TryGetValue(collectorOutletKey, out var bedInRaw)
                && bedInRaw is MoistAirState bedIn
                && step.PortStates.TryGetValue(bedOutletKey, out var bedOutRaw)
                && bedOutRaw is MoistAirState bedOut)
            {
                sawBedPair = true;
                // Adsorption: inlet vapor > outlet; desorption: outlet > inlet.
                var desorptionRate = bedOut.WaterVaporMassFlowKgPerSecond - bedIn.WaterVaporMassFlowKgPerSecond;
                desorbedKg += Math.Max(0.0, desorptionRate) * dt;
            }
        }

        double? electricEnergyJ = (sawBus || peltierProxyJ > 0.0) ? electricJ : null;
        double? incidentJ = sawSolar ? incidentSolarJ : null;
        double? ambientKg = sawAmbient ? ambientMoistureKg : null;
        double? desorbed = sawBedPair ? desorbedKg : null;

        var electricKwh = electricEnergyJ is { } eJ && eJ > 0.0 ? eJ / JoulesPerKilowattHour : (double?)null;
        var solarKwh = incidentJ is { } sJ && sJ > 0.0 ? sJ / JoulesPerKilowattHour : (double?)null;

        return new AwgPerformanceKpis
        {
            LitersPerDay = litersPerDay,
            ElectricEnergyConsumedJ = electricEnergyJ,
            BusElectricalEnergyJ = sawBus ? busJ : null,
            PeltierElectricalProxyEnergyJ = peltierProxyJ > 0.0 || sawBus ? peltierProxyJ : null,
            IncidentSolarPrimaryEnergyJ = incidentJ,
            AmbientMoistureIntakeKg = ambientKg,
            DesorbedWaterMassKg = desorbed,
            SolarCollectorApertureAreaM2 = apertureM2 > 0.0 ? apertureM2 : null,
            LitersPerKwhElectric = RatioOrNull(liters, electricKwh),
            LitersPerKwhSolarPrimary = RatioOrNull(liters, solarKwh),
            LitersPerDayPerSquareMeterAperture = RatioOrNull(
                litersPerDay,
                apertureM2 > 0.0 ? apertureM2 : null),
            WaterRecoveryFraction = RatioOrNull(waterKg, ambientKg is > 0.0 ? ambientKg : null),
            DesorptionCaptureFraction = RatioOrNull(waterKg, desorbed is > 0.0 ? desorbed : null),
            WattHoursElectricPerLiter = InverseWhPerLiter(liters, electricEnergyJ)
        };
    }

    /// <summary>Wh_e/L from integrated electrical energy; null when water or energy ≤ 0.</summary>
    public static double? InverseWhPerLiter(double liters, double? electricEnergyJ)
    {
        if (liters <= 0.0 || electricEnergyJ is not { } eJ || eJ <= 0.0)
        {
            return null;
        }

        return (eJ / 3600.0) / liters;
    }

    /// <summary>Safe ratio; null when numerator unavailable or denominator missing/≤0.</summary>
    public static double? RatioOrNull(double? numerator, double? denominator)
    {
        if (numerator is not { } num || denominator is not { } den || den <= 0.0)
        {
            return null;
        }

        return num / den;
    }

    private static double TryElectricalW(IReadOnlyDictionary<string, object?> ports, string key)
        => ports.TryGetValue(key, out var raw) && raw is ElectricalPowerState e
            ? Math.Max(0.0, e.PowerW)
            : 0.0;

    private static double TryHeatW(IReadOnlyDictionary<string, object?> ports, string key)
        => ports.TryGetValue(key, out var raw) && raw is HeatFlowState h
            ? Math.Max(0.0, h.HeatFlowW)
            : 0.0;
}

/// <summary>Additive water/energy comparison KPIs for an AWG run.</summary>
public sealed record AwgPerformanceKpis
{
    public double? LitersPerDay { get; init; }

    /// <summary>Σ (bus load + Peltier cooling proxy) · Δt (J).</summary>
    public double? ElectricEnergyConsumedJ { get; init; }

    public double? BusElectricalEnergyJ { get; init; }

    /// <summary>Σ condenser-cooling heat request · Δt (J); COP≈1 electrical proxy.</summary>
    public double? PeltierElectricalProxyEnergyJ { get; init; }

    /// <summary>
    /// Incident solar on the thermal-collector aperture only (J). Does not include PV area,
    /// useful collector heat, or recovered internal heat.
    /// </summary>
    public double? IncidentSolarPrimaryEnergyJ { get; init; }

    public double? AmbientMoistureIntakeKg { get; init; }

    public double? DesorbedWaterMassKg { get; init; }

    public double? SolarCollectorApertureAreaM2 { get; init; }

    /// <summary>L/kWh_electric (KPI-001).</summary>
    public double? LitersPerKwhElectric { get; init; }

    /// <summary>L/kWh_solar_primary (KPI-002); denominator = incident aperture solar.</summary>
    public double? LitersPerKwhSolarPrimary { get; init; }

    /// <summary>L/day/m² solar aperture (KPI-003).</summary>
    public double? LitersPerDayPerSquareMeterAperture { get; init; }

    /// <summary>Collected water / ambient moisture intake (KPI-004).</summary>
    public double? WaterRecoveryFraction { get; init; }

    /// <summary>Collected water / desorbed bed water when desorption occurred.</summary>
    public double? DesorptionCaptureFraction { get; init; }

    /// <summary>Wh_electric/L from integrated electrical energy.</summary>
    public double? WattHoursElectricPerLiter { get; init; }
}
