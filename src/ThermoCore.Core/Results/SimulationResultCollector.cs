using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.Core.Results;

public enum ResultCapturePolicy
{
    EveryStep,
    FixedInterval,
    SummaryOnly
}

public enum SimulationRunStatus
{
    Completed,
    CompletedWithWarnings,
    Cancelled,
    FailedValidation,
    FailedConvergence,
    FailedRuntime
}

public sealed record SimulationRunMetadata
{
    public required string ResultFormatVersion { get; init; }

    public required DateTimeOffset StartTimeUtc { get; init; }

    public required TimeSpan Duration { get; init; }

    public required TimeSpan TimeStep { get; init; }

    public required int CapturedStepCount { get; init; }

    public required int TotalStepCount { get; init; }

    public required ResultCapturePolicy CapturePolicy { get; init; }
}

public sealed record ResultChannelDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string QuantityType { get; init; }

    public required string Unit { get; init; }

    public required string ComponentId { get; init; }

    public string Description { get; init; } = string.Empty;
}

public sealed record ResultTimeSeriesChannel
{
    public required ResultChannelDefinition Definition { get; init; }

    public required IReadOnlyList<double> Values { get; init; }
}

public sealed record SimulationSummary
{
    public required bool Succeeded { get; init; }

    public required SimulationRunStatus Status { get; init; }

    public required double MaxAbsEnergyResidualJ { get; init; }

    public required double MaxAbsWaterResidualKg { get; init; }

    public required double MaxAbsDryAirResidualKg { get; init; }

    public required double AggregatedEnergyResidualJ { get; init; }

    public required double AggregatedWaterResidualKg { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public IReadOnlyDictionary<string, double> ScalarMetrics { get; init; }
        = new Dictionary<string, double>(StringComparer.Ordinal);
}

/// <summary>
/// Collected simulation result with optional downsampled time-series channels
/// (docs/04_Simulation/16_SimulationEngine.md §19–§20, docs/05_Product/29_ResultFormats.md).
/// </summary>
public sealed record SimulationResult
{
    public required SimulationRunMetadata Metadata { get; init; }

    public required SimulationRunStatus Status { get; init; }

    public required SimulationSummary Summary { get; init; }

    public required IReadOnlyList<ResultTimeSeriesChannel> Channels { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }

    public required ConservationBalance AggregatedBalance { get; init; }
}

/// <summary>
/// Builds <see cref="SimulationResult"/> from an engine run (GRAPH-010).
/// </summary>
public static class SimulationResultCollector
{
    public const string ResultFormatVersion = "1.0";

    public static SimulationResult Collect(
        SimulationRunResult run,
        SimulationRequest request,
        ResultCapturePolicy capturePolicy = ResultCapturePolicy.EveryStep,
        int fixedIntervalSteps = 1)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(request);
        if (capturePolicy == ResultCapturePolicy.FixedInterval && fixedIntervalSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedIntervalSteps), "Fixed interval must be positive.");
        }

        var totalSteps = run.Steps.Count;
        var selectedSteps = SelectSteps(run.Steps, capturePolicy, fixedIntervalSteps);
        var channels = capturePolicy == ResultCapturePolicy.SummaryOnly
            ? Array.Empty<ResultTimeSeriesChannel>()
            : BuildChannels(selectedSteps);

        var warningCount = run.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        var errorCount = run.Diagnostics.Count(d =>
            d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical);

        var status = ResolveStatus(run, warningCount, errorCount);
        var maxEnergy = 0.0;
        var maxWater = 0.0;
        var maxDryAir = 0.0;
        foreach (var step in run.Steps)
        {
            maxEnergy = Math.Max(maxEnergy, Math.Abs(step.SystemBalance.EnergyResidualJ));
            maxWater = Math.Max(maxWater, Math.Abs(step.SystemBalance.WaterMassResidualKg));
            maxDryAir = Math.Max(maxDryAir, Math.Abs(step.SystemBalance.DryAirMassResidualKg));
        }

        var scalars = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["steps.total"] = totalSteps,
            ["steps.captured"] = selectedSteps.Count,
            ["balance.energy.residualAbsMaxJ"] = maxEnergy,
            ["balance.water.residualAbsMaxKg"] = maxWater
        };

        foreach (var channel in channels)
        {
            if (channel.Values.Count == 0)
            {
                continue;
            }

            if (channel.Definition.Id.EndsWith(".liquidWater.massFlow", StringComparison.Ordinal)
                || channel.Definition.Id.Contains("liquid_out.massFlow", StringComparison.Ordinal))
            {
                scalars[$"integral.{channel.Definition.Id}"] =
                    channel.Values.Sum() * request.TimeStep.TotalSeconds;
            }
        }

        return new SimulationResult
        {
            Metadata = new SimulationRunMetadata
            {
                ResultFormatVersion = ResultFormatVersion,
                StartTimeUtc = request.StartTimeUtc,
                Duration = request.Duration,
                TimeStep = request.TimeStep,
                CapturedStepCount = selectedSteps.Count,
                TotalStepCount = totalSteps,
                CapturePolicy = capturePolicy
            },
            Status = status,
            Summary = new SimulationSummary
            {
                Succeeded = run.Succeeded,
                Status = status,
                MaxAbsEnergyResidualJ = maxEnergy,
                MaxAbsWaterResidualKg = maxWater,
                MaxAbsDryAirResidualKg = maxDryAir,
                AggregatedEnergyResidualJ = run.AggregatedBalance.EnergyResidualJ,
                AggregatedWaterResidualKg = run.AggregatedBalance.WaterMassResidualKg,
                WarningCount = warningCount,
                ErrorCount = errorCount,
                ScalarMetrics = scalars
            },
            Channels = channels,
            Diagnostics = run.Diagnostics,
            AggregatedBalance = run.AggregatedBalance
        };
    }

    private static SimulationRunStatus ResolveStatus(
        SimulationRunResult run,
        int warningCount,
        int errorCount)
    {
        if (!run.Succeeded)
        {
            if (run.Diagnostics.Any(d => d.Code.Contains("CONVERGENCE", StringComparison.OrdinalIgnoreCase)))
            {
                return SimulationRunStatus.FailedConvergence;
            }

            if (run.Diagnostics.Any(d => d.Code.Contains("VALIDATION", StringComparison.OrdinalIgnoreCase)
                || d.Code.StartsWith("GRAPH.", StringComparison.Ordinal)))
            {
                return SimulationRunStatus.FailedValidation;
            }

            return SimulationRunStatus.FailedRuntime;
        }

        return warningCount > 0 || errorCount > 0
            ? SimulationRunStatus.CompletedWithWarnings
            : SimulationRunStatus.Completed;
    }

    private static IReadOnlyList<SimulationStepResult> SelectSteps(
        IReadOnlyList<SimulationStepResult> steps,
        ResultCapturePolicy policy,
        int fixedIntervalSteps)
    {
        if (steps.Count == 0 || policy == ResultCapturePolicy.SummaryOnly)
        {
            return Array.Empty<SimulationStepResult>();
        }

        if (policy == ResultCapturePolicy.EveryStep)
        {
            return steps;
        }

        var selected = new List<SimulationStepResult>();
        for (var i = 0; i < steps.Count; i++)
        {
            if (i % fixedIntervalSteps == 0 || i == steps.Count - 1)
            {
                selected.Add(steps[i]);
            }
        }

        return selected;
    }

    private static IReadOnlyList<ResultTimeSeriesChannel> BuildChannels(
        IReadOnlyList<SimulationStepResult> steps)
    {
        if (steps.Count == 0)
        {
            return Array.Empty<ResultTimeSeriesChannel>();
        }

        var builders = new Dictionary<string, (ResultChannelDefinition Definition, List<double> Values)>(
            StringComparer.Ordinal);

        foreach (var step in steps)
        {
            AppendBalanceSamples(builders, step);
            foreach (var (portKey, state) in step.PortStates)
            {
                AppendPortSamples(builders, portKey, state);
            }
        }

        return builders.Values
            .OrderBy(b => b.Definition.Id, StringComparer.Ordinal)
            .Select(b => new ResultTimeSeriesChannel
            {
                Definition = b.Definition,
                Values = b.Values
            })
            .ToArray();
    }

    private static void AppendBalanceSamples(
        Dictionary<string, (ResultChannelDefinition Definition, List<double> Values)> builders,
        SimulationStepResult step)
    {
        AddSample(builders, "balance.energy.residual", "Energy residual", "Energy", "J", "system",
            step.SystemBalance.EnergyResidualJ);
        AddSample(builders, "balance.water.residual", "Water residual", "Mass", "kg", "system",
            step.SystemBalance.WaterMassResidualKg);
        AddSample(builders, "balance.dryAir.residual", "Dry-air residual", "Mass", "kg", "system",
            step.SystemBalance.DryAirMassResidualKg);
        AddSample(builders, "balance.electrical.residual", "Electrical residual", "Energy", "J", "system",
            step.SystemBalance.ElectricalEnergyResidualJ);
    }

    private static void AppendPortSamples(
        Dictionary<string, (ResultChannelDefinition Definition, List<double> Values)> builders,
        string portKey,
        object? state)
    {
        var componentId = portKey.Contains('.', StringComparison.Ordinal)
            ? portKey[..portKey.IndexOf('.')]
            : portKey;

        switch (state)
        {
            case MoistAirState air:
                AddSample(builders, $"{portKey}.temperature", "Temperature", "Temperature", "K", componentId, air.TemperatureK);
                AddSample(builders, $"{portKey}.humidityRatio", "Humidity ratio", "HumidityRatio", "kg/kg", componentId, air.HumidityRatioKgPerKgDryAir);
                AddSample(builders, $"{portKey}.relativeHumidity", "Relative humidity", "Fraction", "1", componentId, air.RelativeHumidityFraction);
                AddSample(builders, $"{portKey}.dryAirMassFlow", "Dry-air mass flow", "MassFlow", "kg/s", componentId, air.DryAirMassFlowKgPerSecond);
                AddSample(builders, $"{portKey}.specificEnthalpy", "Specific enthalpy", "SpecificEnergy", "J/kg", componentId, air.SpecificEnthalpyJPerKgDryAir);
                break;
            case HeatFlowState heat:
                AddSample(builders, $"{portKey}.heatFlow", "Heat flow", "Power", "W", componentId, heat.HeatFlowW);
                AddSample(builders, $"{portKey}.temperature", "Temperature", "Temperature", "K", componentId, heat.TemperatureK);
                break;
            case ElectricalPowerState electrical:
                AddSample(builders, $"{portKey}.power", "Electrical power", "Power", "W", componentId, electrical.PowerW);
                break;
            case LiquidWaterState liquid:
                AddSample(builders, $"{portKey}.massFlow", "Liquid water mass flow", "MassFlow", "kg/s", componentId, liquid.MassFlowKgPerSecond);
                AddSample(builders, $"{portKey}.temperature", "Temperature", "Temperature", "K", componentId, liquid.TemperatureK);
                break;
            case SolarIrradianceState solar:
                AddSample(builders, $"{portKey}.irradiance", "Solar irradiance", "Irradiance", "W/m^2", componentId, solar.IrradianceWPerM2);
                break;
        }
    }

    private static void AddSample(
        Dictionary<string, (ResultChannelDefinition Definition, List<double> Values)> builders,
        string id,
        string displayName,
        string quantityType,
        string unit,
        string componentId,
        double value)
    {
        if (!builders.TryGetValue(id, out var entry))
        {
            entry = (
                new ResultChannelDefinition
                {
                    Id = id,
                    DisplayName = displayName,
                    QuantityType = quantityType,
                    Unit = unit,
                    ComponentId = componentId
                },
                []);
            builders[id] = entry;
        }

        entry.Values.Add(value);
    }
}
