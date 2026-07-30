using System.Security.Cryptography;
using System.Text;
using ThermoCore.Core.Components;
using ThermoCore.Core.Components.Adsorption;
using ThermoCore.Core.Components.Power;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Environment;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Simulation;

namespace ThermoCore.AWG.Topology;

/// <summary>
/// Builds the AWG V3 MVP moist-air, liquid-water and optional electrical graphs
/// (docs/04_Simulation/15_SystemTopology.md). Supports acyclic, recirculation, and heat-recovery paths.
/// </summary>
public sealed class AwgV3SystemGraphBuilder : IAwgSystemGraphBuilder
{
    private readonly IPsychrometricCalculator _calculator;

    public AwgV3SystemGraphBuilder(IPsychrometricCalculator? calculator = null)
    {
        _calculator = calculator ?? new PsychrometricCalculator();
    }

    public AwgBuiltSystem Build(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        IWeatherProvider? weatherProvider = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(initialState);

        configuration.Validate();
        initialState.Validate(configuration);

        var diagnostics = ValidateBuilderSupport(configuration);
        if (diagnostics.Count > 0)
        {
            throw new AwgConfigurationException(
                "AWG configuration is not supported by the current graph builder.",
                diagnostics);
        }

        var components = new List<ISimulationComponent>();
        var connections = new List<PhysicalConnection>();
        var loops = new List<SimulationLoopDefinition>();
        var externalInputs = new Dictionary<string, object?>(StringComparer.Ordinal);

        BuildAirflowAndWater(
            configuration,
            initialState,
            weatherProvider,
            components,
            connections,
            loops,
            externalInputs);

        if (configuration.Topology.EnableElectricalSubsystem)
        {
            BuildElectrical(configuration, initialState, weatherProvider, components, connections);
        }

        var graph = new SimulationGraph(components, connections);
        var validation = graph.Validate();
        if (!validation.IsValid)
        {
            throw new AwgConfigurationException(
                "Built AWG graph failed Core validation.",
                validation.Diagnostics);
        }

        var metadata = new AwgTopologyMetadata
        {
            TopologyId = configuration.TopologyId,
            TopologyVersion = configuration.TopologyVersion,
            EnableRecirculation = configuration.Topology.EnableRecirculation,
            EnableHeatRecovery = configuration.Topology.EnableHeatRecovery,
            EnableElectricalSubsystem = configuration.Topology.EnableElectricalSubsystem,
            ComponentModelSelections = configuration.Topology.ComponentModelSelections,
            GraphFingerprint = ComputeFingerprint(configuration, connections)
        };

        return new AwgBuiltSystem
        {
            Graph = graph,
            Metadata = metadata,
            Configuration = configuration,
            InitialState = initialState,
            Loops = loops,
            ExternalInputs = externalInputs
        };
    }

    private static List<SimulationDiagnostic> ValidateBuilderSupport(AwgSystemConfiguration configuration)
    {
        var diagnostics = new List<SimulationDiagnostic>();
        var topology = configuration.Topology;

        if (topology.EnableHeatRecovery && topology.EnableRecirculation)
        {
            diagnostics.Add(Error(
                "AWG.HEAT_RECOVERY_WITH_RECIRCULATION_UNSUPPORTED",
                "Heat recovery and recirculation cannot both be enabled in the MVP builder."));
        }

        if (topology.EnablePvRearAirChannel)
        {
            diagnostics.Add(Error(
                "AWG.PV_REAR_CHANNEL_UNSUPPORTED",
                "PV rear-air channel coupling is not enabled in the MVP builder."));
        }

        RequireModel(
            diagnostics,
            topology,
            AwgV3TopologyIds.ProcessFan,
            AwgV3TopologyIds.ModelIds.PrescribedFlowFan);
        RequireModel(
            diagnostics,
            topology,
            AwgV3TopologyIds.PeltierHotSideHx,
            AwgV3TopologyIds.ModelIds.MoistAirPassThrough);
        RequireModel(
            diagnostics,
            topology,
            AwgV3TopologyIds.SolarCollector,
            AwgV3TopologyIds.ModelIds.DynamicLumpedCollector);
        RequireModel(
            diagnostics,
            topology,
            AwgV3TopologyIds.SilicaGelBed,
            AwgV3TopologyIds.ModelIds.SilicaGelLdfLinear);
        RequireModel(
            diagnostics,
            topology,
            AwgV3TopologyIds.Condenser,
            AwgV3TopologyIds.ModelIds.CondenserBypassFactor);
        RequireModel(
            diagnostics,
            topology,
            AwgV3TopologyIds.WaterTank,
            AwgV3TopologyIds.ModelIds.WaterTankInventory);

        if (topology.EnableRecirculation)
        {
            RequireModel(
                diagnostics,
                topology,
                AwgV3TopologyIds.FreshAirMixer,
                AwgV3TopologyIds.ModelIds.MoistAirMixer);
            RequireModel(
                diagnostics,
                topology,
                AwgV3TopologyIds.RecirculationSplitter,
                AwgV3TopologyIds.ModelIds.MoistAirSplitter);
        }

        if (topology.EnableHeatRecovery)
        {
            RequireModel(
                diagnostics,
                topology,
                AwgV3TopologyIds.HeatRecovery,
                AwgV3TopologyIds.ModelIds.SensibleHeatRecoveryPrescribed);
        }

        if (topology.EnableElectricalSubsystem)
        {
            RequireModel(
                diagnostics,
                topology,
                AwgV3TopologyIds.PvPanel,
                AwgV3TopologyIds.ModelIds.ConstantEfficiencyPv);
            RequireModel(
                diagnostics,
                topology,
                AwgV3TopologyIds.PowerManager,
                AwgV3TopologyIds.ModelIds.PowerManagerWithBattery);
        }

        return diagnostics;
    }

    private void BuildAirflowAndWater(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        IWeatherProvider? weatherProvider,
        List<ISimulationComponent> components,
        List<PhysicalConnection> connections,
        List<SimulationLoopDefinition> loops,
        Dictionary<string, object?> externalInputs)
    {
        var ambient = configuration.Ambient;
        var recirculationFraction = configuration.Topology.EnableRecirculation
            ? initialState.RecirculationFraction
            : 0.0;
        var processFlow = configuration.Fan.DryAirMassFlowKgPerSecond;
        var freshFlow = processFlow * (1.0 - recirculationFraction);
        var enableHeatRecovery = configuration.Topology.EnableHeatRecovery;

        var freshInlet = _calculator.CreateFromRelativeHumidity(
            ambient.TemperatureK,
            ambient.PressurePa,
            ambient.RelativeHumidityFraction,
            freshFlow);

        var silicaInitial = SilicaGelState.Create(
            dryAdsorbentMassKg: configuration.SilicaGel.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: initialState.SilicaGelLoadingKgPerKg,
            bedTemperatureK: initialState.SilicaGelTemperatureK,
            maximumWaterLoadingKgPerKgDryAdsorbent: configuration.SilicaGel.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: configuration.SilicaGel.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: configuration.SilicaGel.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: configuration.SilicaGel.BedHousingThermalCapacityJPerK);

        var isotherm = GenericPolynomialIsotherm.CreateLinear(
            configuration.SilicaGel.MaximumWaterLoadingKgPerKgDryAdsorbent);

        components.Add(CreateAmbientSource(weatherProvider, freshFlow, freshInlet));
        components.Add(new PrescribedFlowFanComponent(
            AwgV3TopologyIds.ProcessFan,
            processFlow,
            configuration.Fan.PressureRisePa,
            configuration.Fan.FanEfficiency,
            configuration.Fan.DriverEfficiency,
            _calculator));
        components.Add(new MoistAirPassThroughComponent(AwgV3TopologyIds.PeltierHotSideHx));
        components.Add(new DynamicLumpedSolarCollectorComponent(
            AwgV3TopologyIds.SolarCollector,
            configuration.SolarCollector.OpticalEfficiencyFraction,
            configuration.SolarCollector.ApertureAreaM2,
            configuration.SolarCollector.EffectiveThermalCapacityJPerK,
            configuration.SolarCollector.AbsorberToAirUaWPerK,
            configuration.SolarCollector.OverallLossCoefficientWPerM2K,
            initialState.SolarCollectorAbsorberTemperatureK,
            ambient.TemperatureK,
            configuration.SolarCollector.IncidenceAngleModifierFraction,
            fallbackIrradianceWPerM2: ambient.SolarIrradianceWPerSquareMeter,
            windSpeedMPerSecond: configuration.SolarCollector.WindSpeedMPerSecond,
            windLossCoefficientWPerM2KPerMps: configuration.SolarCollector.WindLossCoefficientWPerM2KPerMps,
            maximumAllowedAbsorberTemperatureK: configuration.SolarCollector.MaximumAllowedAbsorberTemperatureK,
            calculator: _calculator));
        components.Add(new SilicaGelBedComponent(
            AwgV3TopologyIds.SilicaGelBed,
            configuration.SilicaGel,
            isotherm,
            silicaInitial,
            _calculator));
        components.Add(new CondenserComponent(
            AwgV3TopologyIds.Condenser,
            configuration.Condenser.BypassFactor,
            configuration.Condenser.DrainageEfficiency,
            configuration.Condenser.FallbackSurfaceTemperatureK,
            configuration.Condenser.FallbackAvailableCoolingPowerW,
            configuration.Condenser.MaximumRetainedFilmKg,
            configuration.Condenser.FilmCarryoverFraction,
            calculator: _calculator));
        components.Add(new ExhaustAirSinkComponent(AwgV3TopologyIds.ExhaustSink));
        components.Add(new WaterTankComponent(
            AwgV3TopologyIds.WaterTank,
            configuration.WaterTank.CapacityKg,
            initialState.WaterTankContentKg,
            configuration.WaterTank.InitialTemperatureK));
        components.Add(CreateSolarSource(
            AwgV3TopologyIds.SolarRadiation,
            weatherProvider,
            ambient.SolarIrradianceWPerSquareMeter));

        if (enableHeatRecovery)
        {
            configuration.HeatRecovery.Validate();
            components.Add(new SensibleHeatRecoveryComponent(
                AwgV3TopologyIds.HeatRecovery,
                configuration.HeatRecovery.EffectivenessFraction,
                configuration.HeatRecovery.BypassFraction,
                calculator: _calculator));
        }

        if (configuration.Topology.EnableRecirculation)
        {
            components.Add(new MoistAirMixerComponent(
                AwgV3TopologyIds.FreshAirMixer,
                ["fresh_in", "recirc_in"],
                _calculator));
            components.Add(new MoistAirSplitterComponent(
                AwgV3TopologyIds.RecirculationSplitter,
                [1.0 - recirculationFraction, recirculationFraction],
                _calculator));

            Connect(connections, AwgV3TopologyIds.AmbientSource, "outlet", AwgV3TopologyIds.FreshAirMixer, "fresh_in");
            Connect(
                connections,
                AwgV3TopologyIds.RecirculationSplitter,
                "outlet_1",
                AwgV3TopologyIds.FreshAirMixer,
                "recirc_in",
                AwgV3TopologyIds.RecirculationTearConnectionId);
            Connect(connections, AwgV3TopologyIds.FreshAirMixer, "outlet", AwgV3TopologyIds.ProcessFan, "inlet");
            ConnectProcessTrain(connections);
            Connect(connections, AwgV3TopologyIds.Condenser, "outlet", AwgV3TopologyIds.RecirculationSplitter, "inlet");
            Connect(connections, AwgV3TopologyIds.RecirculationSplitter, "outlet_0", AwgV3TopologyIds.ExhaustSink, "inlet");
            Connect(connections, AwgV3TopologyIds.Condenser, "liquid_out", AwgV3TopologyIds.WaterTank, "inlet");

            var recircGuess = _calculator.CreateFromRelativeHumidity(
                ambient.TemperatureK,
                ambient.PressurePa,
                ambient.RelativeHumidityFraction,
                processFlow * recirculationFraction);
            externalInputs[$"{AwgV3TopologyIds.FreshAirMixer}.recirc_in"] = recircGuess;
            loops.Add(new SimulationLoopDefinition
            {
                Id = "awg-recirculation",
                TearConnectionId = AwgV3TopologyIds.RecirculationTearConnectionId,
                RelaxationFactor = 0.7,
                MaximumIterations = 50
            });
        }
        else if (enableHeatRecovery)
        {
            Connect(connections, AwgV3TopologyIds.AmbientSource, "outlet", AwgV3TopologyIds.HeatRecovery, "cold_in");
            Connect(connections, AwgV3TopologyIds.HeatRecovery, "cold_out", AwgV3TopologyIds.ProcessFan, "inlet");
            ConnectProcessTrain(connections);
            Connect(
                connections,
                AwgV3TopologyIds.Condenser,
                "outlet",
                AwgV3TopologyIds.HeatRecovery,
                "hot_in",
                AwgV3TopologyIds.HeatRecoveryTearConnectionId);
            Connect(connections, AwgV3TopologyIds.HeatRecovery, "hot_out", AwgV3TopologyIds.ExhaustSink, "inlet");
            Connect(connections, AwgV3TopologyIds.Condenser, "liquid_out", AwgV3TopologyIds.WaterTank, "inlet");

            var hotGuess = _calculator.CreateFromRelativeHumidity(
                ambient.TemperatureK,
                ambient.PressurePa,
                ambient.RelativeHumidityFraction,
                processFlow);
            externalInputs[$"{AwgV3TopologyIds.HeatRecovery}.hot_in"] = hotGuess;
            loops.Add(new SimulationLoopDefinition
            {
                Id = "awg-heat-recovery",
                TearConnectionId = AwgV3TopologyIds.HeatRecoveryTearConnectionId,
                RelaxationFactor = 0.7,
                MaximumIterations = 50
            });
        }
        else
        {
            Connect(connections, AwgV3TopologyIds.AmbientSource, "outlet", AwgV3TopologyIds.ProcessFan, "inlet");
            ConnectProcessTrain(connections);
            Connect(connections, AwgV3TopologyIds.Condenser, "outlet", AwgV3TopologyIds.ExhaustSink, "inlet");
            Connect(connections, AwgV3TopologyIds.Condenser, "liquid_out", AwgV3TopologyIds.WaterTank, "inlet");
        }
    }

    private static void ConnectProcessTrain(List<PhysicalConnection> connections)
    {
        Connect(connections, AwgV3TopologyIds.ProcessFan, "outlet", AwgV3TopologyIds.PeltierHotSideHx, "inlet");
        Connect(connections, AwgV3TopologyIds.PeltierHotSideHx, "outlet", AwgV3TopologyIds.SolarCollector, "inlet");
        Connect(connections, AwgV3TopologyIds.SolarRadiation, "outlet", AwgV3TopologyIds.SolarCollector, "solar");
        Connect(connections, AwgV3TopologyIds.SolarCollector, "outlet", AwgV3TopologyIds.SilicaGelBed, "inlet");
        Connect(connections, AwgV3TopologyIds.SilicaGelBed, "outlet", AwgV3TopologyIds.Condenser, "inlet");
    }

    private ISimulationComponent CreateAmbientSource(
        IWeatherProvider? weatherProvider,
        double freshFlow,
        MoistAirState freshInlet)
        => weatherProvider is null
            ? new AmbientAirSourceComponent(AwgV3TopologyIds.AmbientSource, freshInlet)
            : new WeatherDrivenAmbientAirSourceComponent(
                AwgV3TopologyIds.AmbientSource,
                weatherProvider,
                freshFlow,
                _calculator);

    private static ISimulationComponent CreateSolarSource(
        string id,
        IWeatherProvider? weatherProvider,
        double fallbackIrradianceWPerM2)
        => weatherProvider is null
            ? new SolarRadiationSourceComponent(id, fallbackIrradianceWPerM2)
            : new WeatherDrivenSolarRadiationSourceComponent(id, weatherProvider);

    private static void BuildElectrical(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        IWeatherProvider? weatherProvider,
        List<ISimulationComponent> components,
        List<PhysicalConnection> connections)
    {
        var batteryState = BatteryState.Create(
            initialState.BatteryStoredEnergyJ,
            configuration.Battery.NominalCapacityJ,
            configuration.Ambient.TemperatureK);

        components.Add(CreateSolarSource(
            AwgV3TopologyIds.PvSolarRadiation,
            weatherProvider,
            configuration.Ambient.SolarIrradianceWPerSquareMeter));
        components.Add(new ConstantEfficiencySolarPanelComponent(
            AwgV3TopologyIds.PvPanel,
            configuration.Pv.EfficiencyFraction,
            configuration.Pv.AreaM2,
            fallbackIrradianceWPerM2: configuration.Ambient.SolarIrradianceWPerSquareMeter));
        components.Add(new PowerManagementComponent(
            AwgV3TopologyIds.PowerManager,
            configuration.Battery,
            configuration.ElectricalLoads,
            batteryState,
            configuration.MpptEfficiencyFraction));
        components.Add(new ElectricalLoadSinkComponent(AwgV3TopologyIds.ElectricalBusSink));
        components.Add(new ElectricalLoadSinkComponent(AwgV3TopologyIds.CurtailmentSink));

        Connect(connections, AwgV3TopologyIds.PvSolarRadiation, "outlet", AwgV3TopologyIds.PvPanel, "solar");
        Connect(connections, AwgV3TopologyIds.PvPanel, "electrical", AwgV3TopologyIds.PowerManager, "generation");
        Connect(connections, AwgV3TopologyIds.PowerManager, "bus", AwgV3TopologyIds.ElectricalBusSink, "inlet");
        Connect(connections, AwgV3TopologyIds.PowerManager, "curtailed", AwgV3TopologyIds.CurtailmentSink, "inlet");
    }

    private static void Connect(
        List<PhysicalConnection> connections,
        string sourceComponentId,
        string sourcePortId,
        string targetComponentId,
        string targetPortId,
        string? connectionId = null)
    {
        connections.Add(new PhysicalConnection
        {
            Id = connectionId ?? $"{sourceComponentId}.{sourcePortId}->{targetComponentId}.{targetPortId}",
            SourceComponentId = sourceComponentId,
            SourcePortId = sourcePortId,
            TargetComponentId = targetComponentId,
            TargetPortId = targetPortId
        });
    }

    private static void RequireModel(
        List<SimulationDiagnostic> diagnostics,
        AwgV3TopologyConfiguration topology,
        string componentId,
        string expectedModelId)
    {
        if (!topology.ComponentModelSelections.TryGetValue(componentId, out var selected)
            || !string.Equals(selected, expectedModelId, StringComparison.Ordinal))
        {
            diagnostics.Add(Error(
                "AWG.UNSUPPORTED_MODEL",
                $"Component '{componentId}' must use model '{expectedModelId}'."));
        }
    }

    private static SimulationDiagnostic Error(string code, string message)
        => new()
        {
            Code = code,
            Severity = DiagnosticSeverity.Error,
            Message = message
        };

    private static string ComputeFingerprint(
        AwgSystemConfiguration configuration,
        IReadOnlyList<PhysicalConnection> connections)
    {
        var builder = new StringBuilder();
        builder.Append(configuration.TopologyId).Append('|')
            .Append(configuration.TopologyVersion).Append('|')
            .Append(configuration.Topology.EnableRecirculation).Append('|')
            .Append(configuration.Topology.EnableHeatRecovery).Append('|')
            .Append(configuration.Topology.EnableElectricalSubsystem).Append('|');

        foreach (var pair in configuration.Topology.ComponentModelSelections.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            builder.Append(pair.Key).Append('=').Append(pair.Value).Append(';');
        }

        foreach (var connection in connections.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            builder.Append(connection.Id).Append(';');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
