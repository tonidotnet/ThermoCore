using System.Security.Cryptography;
using System.Text;
using ThermoCore.Core.Components;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.App2.SolarAirHeater;

/// <summary>
/// Builds ambient → fan → constant-efficiency collector → exhaust using Core components only.
/// </summary>
public sealed class SolarAirHeaterGraphBuilder
{
    private readonly IPsychrometricCalculator _calculator;

    public SolarAirHeaterGraphBuilder(IPsychrometricCalculator? calculator = null)
    {
        _calculator = calculator ?? new PsychrometricCalculator();
    }

    public SolarAirHeaterBuiltSystem Build(SolarAirHeaterConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        configuration = configuration.Validate();

        var ambient = _calculator.CreateFromRelativeHumidity(
            configuration.AmbientTemperatureK,
            configuration.AmbientPressurePa,
            configuration.AmbientRelativeHumidityFraction,
            configuration.DryAirMassFlowKgPerSecond);

        var components = new ISimulationComponent[]
        {
            new AmbientAirSourceComponent(SolarAirHeaterTopologyIds.AmbientSource, ambient),
            new PrescribedFlowFanComponent(
                SolarAirHeaterTopologyIds.Fan,
                configuration.DryAirMassFlowKgPerSecond,
                configuration.FanPressureRisePa),
            new SolarRadiationSourceComponent(
                SolarAirHeaterTopologyIds.SolarRadiation,
                configuration.SolarIrradianceWPerM2),
            new ConstantEfficiencySolarCollectorComponent(
                SolarAirHeaterTopologyIds.Collector,
                configuration.CollectorEfficiencyFraction,
                configuration.CollectorApertureAreaM2,
                fallbackIrradianceWPerM2: configuration.SolarIrradianceWPerM2,
                calculator: _calculator),
            new ExhaustAirSinkComponent(SolarAirHeaterTopologyIds.Exhaust)
        };

        var connections = new[]
        {
            Connect(
                SolarAirHeaterTopologyIds.AmbientSource,
                "outlet",
                SolarAirHeaterTopologyIds.Fan,
                "inlet"),
            Connect(
                SolarAirHeaterTopologyIds.Fan,
                "outlet",
                SolarAirHeaterTopologyIds.Collector,
                "inlet"),
            Connect(
                SolarAirHeaterTopologyIds.SolarRadiation,
                "outlet",
                SolarAirHeaterTopologyIds.Collector,
                "solar"),
            Connect(
                SolarAirHeaterTopologyIds.Collector,
                "outlet",
                SolarAirHeaterTopologyIds.Exhaust,
                "inlet")
        };

        return new SolarAirHeaterBuiltSystem
        {
            Graph = new SimulationGraph(components, connections),
            Configuration = configuration,
            GraphFingerprint = Fingerprint(configuration)
        };
    }

    private static PhysicalConnection Connect(
        string sourceComponentId,
        string sourcePortId,
        string targetComponentId,
        string targetPortId)
        => new()
        {
            Id = $"{sourceComponentId}.{sourcePortId}->{targetComponentId}.{targetPortId}",
            SourceComponentId = sourceComponentId,
            SourcePortId = sourcePortId,
            TargetComponentId = targetComponentId,
            TargetPortId = targetPortId
        };

    private static string Fingerprint(SolarAirHeaterConfiguration configuration)
    {
        var payload = new StringBuilder()
            .Append(SolarAirHeaterTopologyIds.TopologyId).Append('|')
            .Append(SolarAirHeaterTopologyIds.TopologyVersion).Append('|')
            .Append(configuration.AmbientTemperatureK).Append('|')
            .Append(configuration.DryAirMassFlowKgPerSecond).Append('|')
            .Append(configuration.CollectorEfficiencyFraction).Append('|')
            .Append(configuration.CollectorApertureAreaM2).Append('|')
            .Append(configuration.SolarIrradianceWPerM2)
            .ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
