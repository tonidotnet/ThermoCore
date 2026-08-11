using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Simulation;

/// <summary>
/// Cooling-system result channels (KPI-005 / R1-002).
/// Null when signals are unavailable; never invents COP values as NaN.
/// </summary>
public static class AwgCoolingMetricsCalculator
{
    /// <summary>
    /// Energy-weighted bare-device COP = Σ Qc / Σ Pe.
    /// Null when either integral is missing or Pe ≤ 0.
    /// </summary>
    public static double? BareCoolingDeviceCop(double? coolingEnergyJ, double? electricalEnergyJ)
        => AwgPerformanceKpiCalculator.RatioOrNull(coolingEnergyJ, electricalEnergyJ);

    public static AwgCoolingMetrics Compute(
        AwgBuiltSystem built,
        AwgSimulationOptions options,
        SimulationRunResult engineResult)
    {
        ArgumentNullException.ThrowIfNull(built);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(engineResult);

        if (engineResult.Steps.Count == 0 || options.TimeStep.TotalSeconds <= 0.0)
        {
            return new AwgCoolingMetrics();
        }

        var dt = options.TimeStep.TotalSeconds;
        var fanW = ResolveFanElectricalPowerW(built);
        var tecMode = ResolveTecMode(built);

        var coolingRequestKey = $"{AwgV3TopologyIds.CondenserCooling}.outlet";
        var bedOutletKey = $"{AwgV3TopologyIds.SilicaGelBed}.outlet";
        var condenserOutletKey = $"{AwgV3TopologyIds.Condenser}.outlet";
        var ambientKey = $"{AwgV3TopologyIds.AmbientSource}.outlet";

        string? tecColdKey = null;
        string? tecHotKey = null;
        string? tecElectricalKey = null;
        if (tecMode.ComponentId is { } tecId)
        {
            tecColdKey = $"{tecId}.cold_heat";
            tecHotKey = $"{tecId}.hot_heat";
            tecElectricalKey = $"{tecId}.electrical";
        }

        var deviceCoolingJ = 0.0;
        var deviceElectricalJ = 0.0;
        var plantThermalJ = 0.0;
        var plantElectricalJ = 0.0;
        var liftSum = 0.0;
        var marginSum = 0.0;
        var coolingSampleCount = 0;
        var sawCoolingActuator = false;
        var sawDelivered = false;
        var sawDeviceElectrical = false;

        foreach (var step in engineResult.Steps)
        {
            var requestW = TryHeatW(step.PortStates, coolingRequestKey);
            var requestTk = TryHeatTemperatureK(step.PortStates, coolingRequestKey);
            if (step.PortStates.ContainsKey(coolingRequestKey))
            {
                sawCoolingActuator = true;
            }

            double deviceQcW;
            double devicePeW;
            double? hotTk = null;
            double? coldTk = requestTk;

            if (tecColdKey is not null
                && step.PortStates.TryGetValue(tecColdKey, out var coldRaw)
                && coldRaw is HeatFlowState coldHeat)
            {
                deviceQcW = Math.Max(0.0, coldHeat.HeatFlowW);
                coldTk = coldHeat.TemperatureK;
                if (tecElectricalKey is not null
                    && step.PortStates.TryGetValue(tecElectricalKey, out var elecRaw)
                    && elecRaw is ElectricalPowerState elec)
                {
                    devicePeW = Math.Max(0.0, elec.PowerW);
                    sawDeviceElectrical = true;
                }
                else if (tecMode.ConfiguredCop is { } configuredCop && configuredCop > 0.0)
                {
                    // Constant-COP models may omit the electrical input port in PortStates;
                    // recover Pe = Qc / COPc when the configured COP is known.
                    devicePeW = deviceQcW / configuredCop;
                    sawDeviceElectrical = deviceQcW > 0.0;
                }
                else
                {
                    devicePeW = 0.0;
                }

                if (tecHotKey is not null
                    && step.PortStates.TryGetValue(tecHotKey, out var hotRaw)
                    && hotRaw is HeatFlowState hotHeat)
                {
                    hotTk = hotHeat.TemperatureK;
                }
            }
            else
            {
                // AWG V3 ControllableHeatSource path: request heat is Qc capacity with COP≈1 Pe proxy.
                deviceQcW = requestW;
                devicePeW = requestW;
                if (requestW > 0.0)
                {
                    sawDeviceElectrical = true;
                }
            }

            var deliveredW = 0.0;
            if (step.PortStates.TryGetValue(bedOutletKey, out var inletRaw)
                && inletRaw is MoistAirState inlet
                && step.PortStates.TryGetValue(condenserOutletKey, out var outletRaw)
                && outletRaw is MoistAirState outlet)
            {
                deliveredW = Math.Max(
                    0.0,
                    inlet.DryAirMassFlowKgPerSecond
                        * (inlet.SpecificEnthalpyJPerKgDryAir - outlet.SpecificEnthalpyJPerKgDryAir));
                sawDelivered = true;

                if (requestW > 1e-9 || deviceQcW > 1e-9 || deliveredW > 1e-9)
                {
                    if (coldTk is { } surfaceK)
                    {
                        marginSum += inlet.DewPointTemperatureK - surfaceK;
                        coolingSampleCount++;

                        if (hotTk is null
                            && step.PortStates.TryGetValue(ambientKey, out var ambientRaw)
                            && ambientRaw is MoistAirState ambient)
                        {
                            // Without a TEC hot face, use ambient as the heat-sink proxy.
                            hotTk = ambient.TemperatureK;
                        }

                        if (hotTk is { } sinkK)
                        {
                            liftSum += sinkK - surfaceK;
                        }
                    }
                }
            }

            deviceCoolingJ += deviceQcW * dt;
            deviceElectricalJ += devicePeW * dt;
            plantThermalJ += deliveredW * dt;

            // Plant electrical = device Pe + process-fan electrical (COOLING_SYSTEM_CONTEXT).
            var stepFanW = (requestW > 1e-9 || deviceQcW > 1e-9 || deliveredW > 1e-9)
                ? fanW
                : 0.0;
            plantElectricalJ += (devicePeW + stepFanW) * dt;
        }

        double? deviceCoolingEnergy = sawCoolingActuator || deviceCoolingJ > 0.0 ? deviceCoolingJ : null;
        double? deviceElectricalEnergy = sawDeviceElectrical ? deviceElectricalJ : null;
        double? plantThermal = sawDelivered ? plantThermalJ : null;
        double? plantElectrical = null;
        if (sawDeviceElectrical || fanW > 0.0)
        {
            plantElectrical = plantElectricalJ;
        }

        return new AwgCoolingMetrics
        {
            CoolingPlantThermalInputJ = plantThermal,
            CoolingPlantElectricalEnergyJ = plantElectrical,
            BareCoolingDeviceCOP = BareCoolingDeviceCop(deviceCoolingEnergy, deviceElectricalEnergy),
            CoolingPlantCOP = AwgPerformanceKpiCalculator.RatioOrNull(plantThermal, plantElectrical),
            AverageTemperatureLiftK = coolingSampleCount > 0 ? liftSum / coolingSampleCount : null,
            AverageDewPointMarginK = coolingSampleCount > 0 ? marginSum / coolingSampleCount : null,
            CoolingActiveSampleCount = coolingSampleCount
        };
    }

    private static (string? ComponentId, double? ConfiguredCop) ResolveTecMode(AwgBuiltSystem built)
    {
        foreach (var component in built.Graph.Components)
        {
            switch (component)
            {
                case AnalyticalPeltierComponent analytical:
                    return (analytical.Id, null);
                case ConstantCopPeltierComponent constantCop:
                    return (constantCop.Id, constantCop.CoolingCop);
            }
        }

        return (null, null);
    }

    private static double ResolveFanElectricalPowerW(AwgBuiltSystem built)
    {
        if (built.Configuration.Topology.EnableElectricalSubsystem)
        {
            var fanLoad = built.Configuration.ElectricalLoads
                .FirstOrDefault(l => string.Equals(l.LoadId, "fan", StringComparison.Ordinal));
            if (fanLoad is not null)
            {
                return Math.Max(0.0, fanLoad.RequestedPowerW);
            }
        }

        if (built.Graph.Components.FirstOrDefault(c => c.Id == AwgV3TopologyIds.ProcessFan)
            is PrescribedFlowFanComponent prescribed)
        {
            return Math.Max(0.0, prescribed.LastElectricalPowerW);
        }

        if (built.Graph.Components.FirstOrDefault(c => c.Id == AwgV3TopologyIds.ProcessFan)
            is CurveBasedFanComponent curve)
        {
            return Math.Max(0.0, curve.LastElectricalPowerW);
        }

        return 0.0;
    }

    private static double TryHeatW(IReadOnlyDictionary<string, object?> ports, string key)
        => ports.TryGetValue(key, out var raw) && raw is HeatFlowState h
            ? Math.Max(0.0, h.HeatFlowW)
            : 0.0;

    private static double? TryHeatTemperatureK(IReadOnlyDictionary<string, object?> ports, string key)
        => ports.TryGetValue(key, out var raw) && raw is HeatFlowState h
            ? h.TemperatureK
            : null;
}

/// <summary>Additive cooling COP / dew-point summary channels.</summary>
public sealed record AwgCoolingMetrics
{
    /// <summary>Σ delivered condenser cooling · Δt (J).</summary>
    public double? CoolingPlantThermalInputJ { get; init; }

    /// <summary>Σ (device Pe + fan Pe during cooling) · Δt (J).</summary>
    public double? CoolingPlantElectricalEnergyJ { get; init; }

    /// <summary>Σ device Qc / Σ device Pe. ~1.0 for the AWG ControllableHeatSource proxy.</summary>
    public double? BareCoolingDeviceCOP { get; init; }

    /// <summary>Plant COP = thermal input / plant electrical (includes fan).</summary>
    public double? CoolingPlantCOP { get; init; }

    /// <summary>Mean (T_hot − T_cold) over cooling-active samples (K).</summary>
    public double? AverageTemperatureLiftK { get; init; }

    /// <summary>Mean (T_dp,in − T_surface) over cooling-active samples (K).</summary>
    public double? AverageDewPointMarginK { get; init; }

    public int CoolingActiveSampleCount { get; init; }
}
