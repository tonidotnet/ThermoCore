using ThermoCore.AWG.Topology;
using ThermoCore.Core.Calibration;

namespace ThermoCore.AWG.Calibration;

/// <summary>Builds bounded calibratable parameters from an AWG system configuration.</summary>
public static class AwgCalibratableParameterCatalog
{
    public static IReadOnlyList<CalibratableParameter> CreateDefault(AwgSystemConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var list = new List<CalibratableParameter>
        {
            new()
            {
                Id = AwgCalibratableParameterIds.CondenserBypassFactor,
                InitialValue = configuration.Condenser.BypassFactor,
                LowerBound = 0.05,
                UpperBound = 0.60
            },
            new()
            {
                Id = AwgCalibratableParameterIds.CondenserDrainageEfficiency,
                InitialValue = configuration.Condenser.DrainageEfficiency,
                LowerBound = 0.50,
                UpperBound = 1.0
            },
            new()
            {
                Id = AwgCalibratableParameterIds.SilicaGelMassTransfer,
                InitialValue = configuration.SilicaGel.ReferenceMassTransferCoefficientPerSecond,
                LowerBound = 0.001,
                UpperBound = 0.10
            },
            new()
            {
                Id = AwgCalibratableParameterIds.SolarCollectorLossCoefficient,
                InitialValue = configuration.SolarCollector.OverallLossCoefficientWPerM2K,
                LowerBound = 1.0,
                UpperBound = 12.0
            }
        };

        if (configuration.Topology.EnableHeatRecovery)
        {
            list.Add(new CalibratableParameter
            {
                Id = AwgCalibratableParameterIds.HeatRecoveryEffectiveness,
                InitialValue = configuration.HeatRecovery.EffectivenessFraction,
                LowerBound = 0.20,
                UpperBound = 0.90
            });
        }

        return list;
    }

    public static IReadOnlyList<CalibratableParameter> Select(
        AwgSystemConfiguration configuration,
        IEnumerable<string> parameterIds)
    {
        var wanted = parameterIds.ToHashSet(StringComparer.Ordinal);
        return CreateDefault(configuration)
            .Where(p => wanted.Contains(p.Id))
            .Select(p => p.Validate())
            .ToArray();
    }

    public static AwgSystemConfiguration Apply(
        AwgSystemConfiguration configuration,
        IReadOnlyDictionary<string, double> values)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(values);

        var condenser = configuration.Condenser;
        if (values.TryGetValue(AwgCalibratableParameterIds.CondenserBypassFactor, out var bypass))
        {
            condenser = condenser with { BypassFactor = bypass };
        }

        if (values.TryGetValue(AwgCalibratableParameterIds.CondenserDrainageEfficiency, out var drainage))
        {
            condenser = condenser with { DrainageEfficiency = drainage };
        }

        var silica = configuration.SilicaGel;
        if (values.TryGetValue(AwgCalibratableParameterIds.SilicaGelMassTransfer, out var k))
        {
            silica = silica with { ReferenceMassTransferCoefficientPerSecond = k };
        }

        var collector = configuration.SolarCollector;
        if (values.TryGetValue(AwgCalibratableParameterIds.SolarCollectorLossCoefficient, out var loss))
        {
            collector = collector with { OverallLossCoefficientWPerM2K = loss };
        }

        var heatRecovery = configuration.HeatRecovery;
        if (values.TryGetValue(AwgCalibratableParameterIds.HeatRecoveryEffectiveness, out var effectiveness))
        {
            heatRecovery = heatRecovery with { EffectivenessFraction = effectiveness };
        }

        return (configuration with
        {
            Condenser = condenser,
            SilicaGel = silica,
            SolarCollector = collector,
            HeatRecovery = heatRecovery
        }).Validate();
    }
}
