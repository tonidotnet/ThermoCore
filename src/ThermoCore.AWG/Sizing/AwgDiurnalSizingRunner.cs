using ThermoCore.AWG.Control;
using ThermoCore.AWG.WeatherProfiles;
using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Balances;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Sizing;

/// <summary>
/// Runs a controlled 24 h summer-diurnal AWG case and sizes PV/battery for water targets
/// by linear scale of measured specific energy (Wh/L).
/// </summary>
public sealed class AwgDiurnalSizingRunner
{
    public static readonly double[] DefaultTargetsLitersPerDay = [0.5, 1.0, 2.0, 3.0];

    private readonly AwgSimulationRunner _simulationRunner;

    public AwgDiurnalSizingRunner(AwgSimulationRunner? simulationRunner = null)
    {
        _simulationRunner = simulationRunner ?? new AwgSimulationRunner();
    }

    public AwgDiurnalSizingReport Run(
        DateTimeOffset? dayStartUtc = null,
        IReadOnlyList<double>? targetsLitersPerDay = null,
        TimeSpan? timeStep = null)
    {
        var start = (dayStartUtc ?? DateTimeOffset.Parse("2026-07-15T00:00:00Z")).ToUniversalTime();
        var targets = targetsLitersPerDay ?? DefaultTargetsLitersPerDay;
        var dt = timeStep ?? TimeSpan.FromSeconds(5);
        var weather = SummerDiurnalWeatherFactory.CreateProvider(start);
        var peakSunHours = SummerDiurnalWeatherFactory.EstimatePeakSunHours();

        var meanAmbientK = UnitConversions.CelsiusToKelvin(26.0);
        var configuration = AwgSystemDefaults.CreateMvpConfiguration(
            enableElectricalSubsystem: true,
            enableHeatRecovery: false);
        configuration = (configuration with
        {
            Ambient = configuration.Ambient with
            {
                TemperatureK = meanAmbientK,
                RelativeHumidityFraction = 0.45,
                SolarIrradianceWPerSquareMeter = SummerDiurnalWeatherFactory.PeakIrradianceWPerM2
            },
            SilicaGel = configuration.SilicaGel with
            {
                AmbientTemperatureK = meanAmbientK,
                DryAdsorbentMassKg = 2.0
            },
            WaterTank = configuration.WaterTank with
            {
                InitialTemperatureK = meanAmbientK
            },
            // Headroom for daytime regeneration + night autonomy.
            Battery = configuration.Battery with
            {
                NominalCapacityJ = 3_600_000.0 * 2.0,
                MaximumChargePowerW = 300.0,
                MaximumDischargePowerW = 300.0
            },
            Pv = configuration.Pv with
            {
                AreaM2 = 1.5,
                RatedPowerW = 270.0
            }
        }).Validate();

        var initial = AwgSystemDefaults.CreateMvpInitialState(configuration) with
        {
            SilicaGelLoadingKgPerKg = configuration.SilicaGel.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            SilicaGelTemperatureK = meanAmbientK,
            SolarCollectorAbsorberTemperatureK = meanAmbientK,
            BatteryStoredEnergyJ = 0.70 * configuration.Battery.NominalCapacityJ
        };

        var xEqNight = configuration.SilicaGel.MaximumWaterLoadingKgPerKgDryAdsorbent
            * SummerDiurnalWeatherFactory.NightRelativeHumidityFraction;
        var adsorbTarget = Math.Max(
            configuration.SilicaGel.MinimumRegeneratedLoadingKgPerKgDryAdsorbent + 0.025,
            0.40 * xEqNight);

        var options = new AwgSimulationOptions
        {
            StartTimeUtc = start,
            Duration = TimeSpan.FromHours(24),
            TimeStep = dt,
            WeatherProvider = weather,
            EnableController = true,
            InitialControllerMode = AwgOperatingMode.Off,
            // Diurnal + soft collector gating accumulates larger per-step residuals than short constant runs.
            BalanceTolerance = new BalanceTolerance
            {
                AbsoluteDryAirMassKg = 1e-6,
                AbsoluteWaterMassKg = 1e-6,
                AbsoluteEnergyJ = Math.Max(5.0, 50.0 * dt.TotalSeconds),
                AbsoluteElectricalEnergyJ = Math.Max(5.0, 50.0 * dt.TotalSeconds),
                Relative = 1e-4
            },
            ControlParameters = RuleBasedAwgController.CreateDefaultParameters() with
            {
                AdsorptionTargetLoadingKgPerKg = adsorbTarget,
                RegenerationEntryLoadingKgPerKg = adsorbTarget,
                RegenerationExitLoadingKgPerKg = 0.025,
                MinimumAdsorptionDrivingForceKgPerKg = 0.004,
                MinimumModeDwell = TimeSpan.FromMinutes(2),
                NominalPeltierPowerRequestW = 120.0,
                CollectorAbsorberTemperatureLimitK = UnitConversions.CelsiusToKelvin(140.0),
                MinimumSolarIrradianceForRegenerationWPerSquareMeter = 200.0
            }
        }.Validate();

        var run = _simulationRunner.Run(configuration, initial.Validate(configuration), options);
        if (!run.EngineResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                run.EngineResult.Diagnostics
                    .Where(d => d.Severity >= ThermoCore.Core.Diagnostics.DiagnosticSeverity.Error)
                    .Select(d => $"{d.Code}:{d.Message}")
                    .Take(8));
            return new AwgDiurnalSizingReport
            {
                DayStartUtc = start,
                SimulationSucceeded = false,
                BaselineWaterLiters = 0,
                BaselineDailyElectricalWh = 0,
                BaselinePvGenerationWh = 0,
                BaselinePeltierElectricalWh = 0,
                BaselineBusLoadWh = 0,
                BaselineNightElectricalWh = 0,
                PeakSunHours = peakSunHours,
                SpecificEnergyWhPerLiter = 0,
                Targets = Array.Empty<AwgDiurnalSizingPointResult>(),
                HourlySamples = Array.Empty<AwgDiurnalHourlySample>(),
                Run = run,
                FailureMessage = errors
            };
        }

        var energy = IntegrateEnergy(run, dt.TotalSeconds);
        var waterKg = run.Summary.FinalWaterTankContentKg ?? 0.0;
        var waterL = Math.Max(0.0, waterKg);
        var specific = waterL > 1e-9 ? energy.TotalElectricalWh / waterL : 0.0;
        var hourly = BuildHourlySamples(run, weather, start, dt.TotalSeconds);

        var sized = targets.Select(target => SizeTarget(
            target,
            waterL,
            energy.TotalElectricalWh,
            energy.NightElectricalWh,
            specific,
            peakSunHours,
            configuration.Pv.EfficiencyFraction,
            configuration.MpptEfficiencyFraction)).ToArray();

        return new AwgDiurnalSizingReport
        {
            DayStartUtc = start,
            SimulationSucceeded = true,
            BaselineWaterLiters = waterL,
            BaselineDailyElectricalWh = energy.TotalElectricalWh,
            BaselinePvGenerationWh = energy.PvGenerationWh,
            BaselinePeltierElectricalWh = energy.PeltierElectricalWh,
            BaselineBusLoadWh = energy.BusLoadWh,
            BaselineNightElectricalWh = energy.NightElectricalWh,
            PeakSunHours = peakSunHours,
            SpecificEnergyWhPerLiter = specific,
            Targets = sized,
            HourlySamples = hourly,
            Run = run
        };
    }

    private static AwgDiurnalSizingPointResult SizeTarget(
        double targetL,
        double baselineL,
        double baselineWh,
        double baselineNightWh,
        double specificWhPerL,
        double peakSunHours,
        double pvEfficiency,
        double mpptEfficiency)
    {
        if (baselineL <= 1e-9 || baselineWh <= 1e-9)
        {
            return new AwgDiurnalSizingPointResult
            {
                TargetLitersPerDay = targetL,
                ScaleFactorVersusBaseline = 0,
                DailyElectricalEnergyWh = 0,
                SpecificEnergyWhPerLiter = specificWhPerL,
                RecommendedPvRatedPowerW = 0,
                RecommendedPvAreaM2 = 0,
                RecommendedBatteryCapacityWh = 0,
                NightElectricalEnergyWh = 0,
                Feasible = false,
                Notes = "Baseline produced negligible water; cannot scale."
            };
        }

        var scale = targetL / baselineL;
        var dailyWh = scale * baselineWh;
        var nightWh = scale * baselineNightWh;
        // System derate covers wiring, dust, temperature (~0.85).
        const double systemDerate = 0.85;
        var psh = Math.Max(peakSunHours, 0.5);
        var pvRatedW = dailyWh / (psh * mpptEfficiency * systemDerate);
        var pvArea = pvEfficiency > 1e-9 ? pvRatedW / (1000.0 * pvEfficiency) : 0.0;
        // Usable SOC window 0.1–0.9, discharge η=0.95, +20% contingency for cloudy morning.
        const double usableSoc = 0.8;
        const double dischargeEta = 0.95;
        const double contingency = 1.20;
        var batteryWh = nightWh * contingency / (usableSoc * dischargeEta);

        return new AwgDiurnalSizingPointResult
        {
            TargetLitersPerDay = targetL,
            ScaleFactorVersusBaseline = scale,
            DailyElectricalEnergyWh = dailyWh,
            SpecificEnergyWhPerLiter = specificWhPerL,
            RecommendedPvRatedPowerW = pvRatedW,
            RecommendedPvAreaM2 = pvArea,
            RecommendedBatteryCapacityWh = batteryWh,
            NightElectricalEnergyWh = nightWh,
            Feasible = true,
            Notes = "Linear scale of 24 h baseline specific energy (Wh/L); re-simulate before procurement."
        };
    }

    private static (
        double TotalElectricalWh,
        double PvGenerationWh,
        double PeltierElectricalWh,
        double BusLoadWh,
        double NightElectricalWh) IntegrateEnergy(AwgSimulationRunResult run, double dtSeconds)
    {
        // PortStates store component *outputs*; bus load is on PowerManager.bus, not the sink inlet.
        var pvKey = $"{AwgV3TopologyIds.PvPanel}.electrical";
        var busKey = $"{AwgV3TopologyIds.PowerManager}.bus";
        var coolingKey = $"{AwgV3TopologyIds.CondenserCooling}.outlet";
        var solarKey = $"{AwgV3TopologyIds.SolarRadiation}.outlet";

        var pvJ = 0.0;
        var busJ = 0.0;
        var peltierJ = 0.0;
        var nightJ = 0.0;

        foreach (var step in run.EngineResult.Steps)
        {
            var pvW = TryElectricalW(step.PortStates, pvKey);
            var busW = TryElectricalW(step.PortStates, busKey);
            var peltierW = TryHeatW(step.PortStates, coolingKey);
            var ghi = TrySolarW(step.PortStates, solarKey);

            pvJ += pvW * dtSeconds;
            busJ += busW * dtSeconds;
            peltierJ += peltierW * dtSeconds;

            // Night / low-sun autonomy budget: bus + Peltier when GHI is negligible.
            if (ghi < 50.0)
            {
                nightJ += (busW + peltierW) * dtSeconds;
            }
        }

        var busWh = busJ / 3600.0;
        var peltierWh = peltierJ / 3600.0;
        return (
            TotalElectricalWh: busWh + peltierWh,
            PvGenerationWh: pvJ / 3600.0,
            PeltierElectricalWh: peltierWh,
            BusLoadWh: busWh,
            NightElectricalWh: nightJ / 3600.0);
    }

    private static IReadOnlyList<AwgDiurnalHourlySample> BuildHourlySamples(
        AwgSimulationRunResult run,
        Core.Environment.IWeatherProvider weather,
        DateTimeOffset start,
        double dtSeconds)
    {
        var coolingKey = $"{AwgV3TopologyIds.CondenserCooling}.outlet";
        var samples = new AwgDiurnalHourlySample[24];

        for (var hour = 0; hour < 24; hour++)
        {
            var hourStart = start.AddHours(hour);
            var hourEnd = hourStart.AddHours(1);
            var weatherMid = weather.GetState(hourStart.AddMinutes(30));
            var peltierSum = 0.0;
            var peltierN = 0;
            var fanOn = 0;
            var modeCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var step in run.EngineResult.Steps)
            {
                var t = start + step.ElapsedTime;
                if (t < hourStart || t >= hourEnd)
                {
                    continue;
                }

                peltierSum += TryHeatW(step.PortStates, coolingKey);
                peltierN++;
            }

            // Mode from decision trace index ≈ step index (one evaluation per step).
            var stepIndexStart = (int)Math.Floor(hour * 3600.0 / dtSeconds);
            var stepIndexEnd = (int)Math.Floor((hour + 1) * 3600.0 / dtSeconds);
            for (var i = stepIndexStart; i < Math.Min(stepIndexEnd, run.ControllerDecisionTrace.Count); i++)
            {
                var mode = run.ControllerDecisionTrace[i].RequestedMode;
                modeCounts[mode] = modeCounts.TryGetValue(mode, out var c) ? c + 1 : 1;
                if (mode is not (nameof(AwgOperatingMode.Off)
                    or nameof(AwgOperatingMode.Fault)
                    or nameof(AwgOperatingMode.ControlledShutdown)
                    or nameof(AwgOperatingMode.Standby)))
                {
                    fanOn++;
                }
            }

            var dominant = modeCounts.Count == 0
                ? "n/a"
                : modeCounts.OrderByDescending(kv => kv.Value).First().Key;
            var modeSteps = Math.Max(1, modeCounts.Values.Sum());

            samples[hour] = new AwgDiurnalHourlySample
            {
                HourOfDay = hour,
                AmbientTemperatureC = UnitConversions.KelvinToCelsius(weatherMid.AmbientTemperatureK),
                RelativeHumidityPercent = weatherMid.RelativeHumidityFraction * 100.0,
                IrradianceWPerM2 = weatherMid.GlobalHorizontalIrradianceWPerM2,
                WaterProducedKg = 0.0,
                DominantMode = dominant,
                MeanPeltierW = peltierN > 0 ? peltierSum / peltierN : 0.0,
                MeanFanOnFraction = fanOn / (double)modeSteps
            };
        }

        // Fill hourly water from condenser liquid_out integration.
        var liquidKey = $"{AwgV3TopologyIds.Condenser}.liquid_out";
        var hourlyWater = new double[24];
        foreach (var step in run.EngineResult.Steps)
        {
            var hour = Math.Clamp((int)Math.Floor(step.ElapsedTime.TotalHours), 0, 23);
            if (step.PortStates.TryGetValue(liquidKey, out var raw)
                && raw is LiquidWaterState liquid)
            {
                hourlyWater[hour] += liquid.MassFlowKgPerSecond * dtSeconds;
            }
        }

        for (var h = 0; h < 24; h++)
        {
            samples[h] = samples[h] with { WaterProducedKg = hourlyWater[h] };
        }

        return samples;
    }

    private static double TryElectricalW(IReadOnlyDictionary<string, object?> ports, string key)
        => ports.TryGetValue(key, out var raw) && raw is ElectricalPowerState e ? Math.Max(0.0, e.PowerW) : 0.0;

    private static double TryHeatW(IReadOnlyDictionary<string, object?> ports, string key)
        => ports.TryGetValue(key, out var raw) && raw is HeatFlowState h ? Math.Max(0.0, h.HeatFlowW) : 0.0;

    private static double TrySolarW(IReadOnlyDictionary<string, object?> ports, string key)
        => ports.TryGetValue(key, out var raw) && raw is SolarIrradianceState s
            ? Math.Max(0.0, s.IrradianceWPerM2)
            : 0.0;
}
