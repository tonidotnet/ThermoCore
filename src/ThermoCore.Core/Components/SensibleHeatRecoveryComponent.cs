using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Two-stream sensible heat recovery with prescribed effectiveness
/// (docs/03_Components/11_HeatRecovery.md §4–§6, HR-002).
/// </summary>
public sealed class SensibleHeatRecoveryComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _effectivenessFraction;
    private readonly double _bypassFraction;
    private readonly bool _allowReverseOperation;
    private readonly bool _enableCondensationRiskDiagnostics;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public SensibleHeatRecoveryComponent(
        string id,
        double effectivenessFraction,
        double bypassFraction = 0.0,
        bool allowReverseOperation = false,
        bool enableCondensationRiskDiagnostics = true,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(effectivenessFraction, nameof(effectivenessFraction));
        if (effectivenessFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(effectivenessFraction), "Effectiveness must be in [0, 1].");
        }

        FiniteNumber.Require(bypassFraction, nameof(bypassFraction));
        if (bypassFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(bypassFraction), "Bypass fraction must be in [0, 1].");
        }

        Id = id;
        _effectivenessFraction = effectivenessFraction;
        _bypassFraction = bypassFraction;
        _allowReverseOperation = allowReverseOperation;
        _enableCondensationRiskDiagnostics = enableCondensationRiskDiagnostics;
        _calculator = calculator ?? new PsychrometricCalculator();

        Ports =
        [
            new PhysicalPort("hot_in", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("cold_in", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("hot_out", id, PortDirection.Output, PhysicalDomain.MoistAir),
            new PhysicalPort("cold_out", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastRecoveredHeatW { get; private set; }

    public double LastEffectivenessFraction { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastRecoveredHeatW = 0.0;
        LastEffectivenessFraction = _effectivenessFraction;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();

        if (!TryGetMoistAir(context, "hot_in", out var hotIn, out var hotError))
        {
            return hotError!;
        }

        if (!TryGetMoistAir(context, "cold_in", out var coldIn, out var coldError))
        {
            return coldError!;
        }

        if (hotIn.DryAirMassFlowKgPerSecond <= 0.0 || coldIn.DryAirMassFlowKgPerSecond <= 0.0)
        {
            return Error(context, "COMPONENT.ZERO_FLOW", "Heat recovery requires positive dry-air mass flow on both sides.");
        }

        var hotCapacity = CapacityRateWPerK(hotIn);
        var coldCapacity = CapacityRateWPerK(coldIn);
        var cMin = Math.Min(hotCapacity, coldCapacity);
        var temperatureDifferenceK = hotIn.TemperatureK - coldIn.TemperatureK;

        // Bypass reduces the exchanged fraction; MVP applies ε_eff = ε (1 - b) to full streams.
        var effectiveness = _effectivenessFraction * (1.0 - _bypassFraction);
        LastEffectivenessFraction = effectiveness;

        double recoveredHeatW;
        if (temperatureDifferenceK <= 0.0 && !_allowReverseOperation)
        {
            recoveredHeatW = 0.0;
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "HEAT_RECOVERY.NO_DRIVING_TEMPERATURE",
                Severity = DiagnosticSeverity.Information,
                Message = "Hot-side inlet is not warmer than cold-side inlet; recovered heat set to zero.",
                ComponentId = Id,
                StepIndex = context.Simulation.StepIndex,
                SimulationTime = context.Simulation.ElapsedTime
            });
        }
        else
        {
            var qMax = cMin * temperatureDifferenceK;
            recoveredHeatW = effectiveness * qMax;

            // Prevent unphysical temperature crossing on the limiting stream.
            if (hotCapacity > 0.0 && coldCapacity > 0.0 && Math.Abs(temperatureDifferenceK) > 1e-12)
            {
                var hotOutletCandidate = hotIn.TemperatureK - recoveredHeatW / hotCapacity;
                var coldOutletCandidate = coldIn.TemperatureK + recoveredHeatW / coldCapacity;
                if ((temperatureDifferenceK > 0.0 && hotOutletCandidate < coldOutletCandidate - 1e-9)
                    || (temperatureDifferenceK < 0.0 && hotOutletCandidate > coldOutletCandidate + 1e-9))
                {
                    var maxNonCrossing = temperatureDifferenceK
                        / (1.0 / hotCapacity + 1.0 / coldCapacity);
                    recoveredHeatW = temperatureDifferenceK > 0.0
                        ? Math.Clamp(maxNonCrossing, 0.0, Math.Max(0.0, qMax))
                        : Math.Clamp(maxNonCrossing, Math.Min(0.0, qMax), 0.0);
                    diagnostics.Add(new SimulationDiagnostic
                    {
                        Code = "HEAT_RECOVERY.TEMPERATURE_CROSSING_LIMIT",
                        Severity = DiagnosticSeverity.Information,
                        Message = "Recovered heat was reduced to prevent temperature crossing.",
                        ComponentId = Id,
                        StepIndex = context.Simulation.StepIndex,
                        SimulationTime = context.Simulation.ElapsedTime
                    });
                }
            }
        }

        var hotOutTemperatureK = hotIn.TemperatureK - recoveredHeatW / hotCapacity;
        var coldOutTemperatureK = coldIn.TemperatureK + recoveredHeatW / coldCapacity;

        var hotOut = _calculator.CreateFromHumidityRatio(
            hotOutTemperatureK,
            hotIn.PressurePa,
            hotIn.HumidityRatioKgPerKgDryAir,
            hotIn.DryAirMassFlowKgPerSecond);

        var coldOut = _calculator.CreateFromHumidityRatio(
            coldOutTemperatureK,
            coldIn.PressurePa,
            coldIn.HumidityRatioKgPerKgDryAir,
            coldIn.DryAirMassFlowKgPerSecond);

        if (_enableCondensationRiskDiagnostics
            && hotOut.TemperatureK < hotIn.DewPointTemperatureK)
        {
            diagnostics.Add(new SimulationDiagnostic
            {
                Code = "HEAT_RECOVERY.CONDENSATION_RISK",
                Severity = DiagnosticSeverity.Warning,
                Message = "Hot-side outlet temperature is below the hot-stream dew point.",
                ComponentId = Id,
                PortId = "hot_out",
                StepIndex = context.Simulation.StepIndex,
                SimulationTime = context.Simulation.ElapsedTime
            });
        }

        LastRecoveredHeatW = recoveredHeatW;

        var energyIn = hotIn.DryAirMassFlowKgPerSecond * hotIn.SpecificEnthalpyJPerKgDryAir
            + coldIn.DryAirMassFlowKgPerSecond * coldIn.SpecificEnthalpyJPerKgDryAir;
        var energyOut = hotOut.DryAirMassFlowKgPerSecond * hotOut.SpecificEnthalpyJPerKgDryAir
            + coldOut.DryAirMassFlowKgPerSecond * coldOut.SpecificEnthalpyJPerKgDryAir;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: hotIn.DryAirMassFlowKgPerSecond + coldIn.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: hotOut.DryAirMassFlowKgPerSecond + coldOut.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: hotIn.WaterVaporMassFlowKgPerSecond + coldIn.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: hotOut.WaterVaporMassFlowKgPerSecond + coldOut.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: energyIn,
            energyOutputW: energyOut,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["hot_out"] = hotOut,
                ["cold_out"] = coldOut
            },
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private static double CapacityRateWPerK(MoistAirState state)
        => state.DryAirMassFlowKgPerSecond
           * (ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK
              + state.HumidityRatioKgPerKgDryAir * ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK);

    private bool TryGetMoistAir(
        ComponentStepContext context,
        string portId,
        out MoistAirState state,
        out ComponentStepResult? error)
    {
        if (context.InputStates.TryGetValue(portId, out var raw) && raw is MoistAirState moist)
        {
            state = moist;
            error = null;
            return true;
        }

        state = null!;
        error = Error(context, "COMPONENT.MISSING_INLET", $"Heat recovery requires MoistAirState on '{portId}'.", portId);
        return false;
    }

    private ComponentStepResult Error(
        ComponentStepContext context,
        string code,
        string message,
        string? portId = null)
        => new()
        {
            Diagnostics =
            [
                new SimulationDiagnostic
                {
                    Code = code,
                    Severity = DiagnosticSeverity.Error,
                    Message = message,
                    ComponentId = Id,
                    PortId = portId,
                    StepIndex = context.Simulation.StepIndex,
                    SimulationTime = context.Simulation.ElapsedTime
                }
            ],
            Balance = ConservationBalance.Empty
        };
}
