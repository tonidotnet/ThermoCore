using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

public enum HeatRecoveryModelType
{
    PrescribedEffectiveness,
    CounterFlowNtu
}

/// <summary>
/// Two-stream sensible heat recovery with prescribed effectiveness or counter-flow ε–NTU
/// (docs/03_Components/11_HeatRecovery.md §4–§7, HR-002/HR-003).
/// </summary>
public sealed class SensibleHeatRecoveryComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly HeatRecoveryModelType _modelType;
    private readonly double _effectivenessFraction;
    private readonly double _uaWPerK;
    private readonly double _bypassFraction;
    private readonly bool _allowReverseOperation;
    private readonly bool _enableCondensationRiskDiagnostics;
    private readonly double _hotReferencePressureDropPa;
    private readonly double _coldReferencePressureDropPa;
    private readonly double _hotReferenceVolumetricFlowM3PerSecond;
    private readonly double _coldReferenceVolumetricFlowM3PerSecond;
    private readonly double _pressureDropExponent;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public SensibleHeatRecoveryComponent(
        string id,
        double effectivenessFraction,
        double bypassFraction = 0.0,
        bool allowReverseOperation = false,
        bool enableCondensationRiskDiagnostics = true,
        double hotReferencePressureDropPa = 0.0,
        double coldReferencePressureDropPa = 0.0,
        double hotReferenceVolumetricFlowM3PerSecond = 0.01,
        double coldReferenceVolumetricFlowM3PerSecond = 0.01,
        double pressureDropExponent = 2.0,
        IPsychrometricCalculator? calculator = null)
        : this(
            id,
            HeatRecoveryModelType.PrescribedEffectiveness,
            effectivenessFraction,
            uaWPerK: 0.0,
            bypassFraction,
            allowReverseOperation,
            enableCondensationRiskDiagnostics,
            hotReferencePressureDropPa,
            coldReferencePressureDropPa,
            hotReferenceVolumetricFlowM3PerSecond,
            coldReferenceVolumetricFlowM3PerSecond,
            pressureDropExponent,
            calculator)
    {
    }

    private SensibleHeatRecoveryComponent(
        string id,
        HeatRecoveryModelType modelType,
        double effectivenessFraction,
        double uaWPerK,
        double bypassFraction,
        bool allowReverseOperation,
        bool enableCondensationRiskDiagnostics,
        double hotReferencePressureDropPa,
        double coldReferencePressureDropPa,
        double hotReferenceVolumetricFlowM3PerSecond,
        double coldReferenceVolumetricFlowM3PerSecond,
        double pressureDropExponent,
        IPsychrometricCalculator? calculator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.Require(bypassFraction, nameof(bypassFraction));
        if (bypassFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(bypassFraction), "Bypass fraction must be in [0, 1].");
        }

        if (modelType == HeatRecoveryModelType.PrescribedEffectiveness)
        {
            FiniteNumber.Require(effectivenessFraction, nameof(effectivenessFraction));
            if (effectivenessFraction is < 0.0 or > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectivenessFraction), "Effectiveness must be in [0, 1].");
            }
        }
        else
        {
            FiniteNumber.RequirePositive(uaWPerK, nameof(uaWPerK));
        }

        FiniteNumber.RequireNonNegative(hotReferencePressureDropPa, nameof(hotReferencePressureDropPa));
        FiniteNumber.RequireNonNegative(coldReferencePressureDropPa, nameof(coldReferencePressureDropPa));
        FiniteNumber.RequirePositive(hotReferenceVolumetricFlowM3PerSecond, nameof(hotReferenceVolumetricFlowM3PerSecond));
        FiniteNumber.RequirePositive(coldReferenceVolumetricFlowM3PerSecond, nameof(coldReferenceVolumetricFlowM3PerSecond));
        FiniteNumber.RequirePositive(pressureDropExponent, nameof(pressureDropExponent));

        Id = id;
        _modelType = modelType;
        _effectivenessFraction = effectivenessFraction;
        _uaWPerK = uaWPerK;
        _bypassFraction = bypassFraction;
        _allowReverseOperation = allowReverseOperation;
        _enableCondensationRiskDiagnostics = enableCondensationRiskDiagnostics;
        _hotReferencePressureDropPa = hotReferencePressureDropPa;
        _coldReferencePressureDropPa = coldReferencePressureDropPa;
        _hotReferenceVolumetricFlowM3PerSecond = hotReferenceVolumetricFlowM3PerSecond;
        _coldReferenceVolumetricFlowM3PerSecond = coldReferenceVolumetricFlowM3PerSecond;
        _pressureDropExponent = pressureDropExponent;
        _calculator = calculator ?? new PsychrometricCalculator();

        Ports =
        [
            new PhysicalPort("hot_in", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("cold_in", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("hot_out", id, PortDirection.Output, PhysicalDomain.MoistAir),
            new PhysicalPort("cold_out", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    /// <summary>
    /// Counter-flow effectiveness–NTU model with overall UA
    /// (docs/03_Components/11_HeatRecovery.md §7 / HR-003).
    /// </summary>
    public static SensibleHeatRecoveryComponent CreateCounterFlowNtu(
        string id,
        double uaWPerK,
        double bypassFraction = 0.0,
        bool allowReverseOperation = false,
        bool enableCondensationRiskDiagnostics = true,
        double hotReferencePressureDropPa = 0.0,
        double coldReferencePressureDropPa = 0.0,
        double hotReferenceVolumetricFlowM3PerSecond = 0.01,
        double coldReferenceVolumetricFlowM3PerSecond = 0.01,
        double pressureDropExponent = 2.0,
        IPsychrometricCalculator? calculator = null)
        => new(
            id,
            HeatRecoveryModelType.CounterFlowNtu,
            effectivenessFraction: 0.0,
            uaWPerK,
            bypassFraction,
            allowReverseOperation,
            enableCondensationRiskDiagnostics,
            hotReferencePressureDropPa,
            coldReferencePressureDropPa,
            hotReferenceVolumetricFlowM3PerSecond,
            coldReferenceVolumetricFlowM3PerSecond,
            pressureDropExponent,
            calculator);

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public HeatRecoveryModelType ModelType => _modelType;

    public double LastRecoveredHeatW { get; private set; }

    public double LastEffectivenessFraction { get; private set; }

    public double LastNtu { get; private set; }

    public double LastCapacityRatio { get; private set; }

    public double LastHotPressureDropPa { get; private set; }

    public double LastColdPressureDropPa { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastRecoveredHeatW = 0.0;
        LastEffectivenessFraction = 0.0;
        LastNtu = 0.0;
        LastCapacityRatio = 0.0;
        LastHotPressureDropPa = 0.0;
        LastColdPressureDropPa = 0.0;
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
        var cMax = Math.Max(hotCapacity, coldCapacity);
        var capacityRatio = cMax > 0.0 ? cMin / cMax : 0.0;
        var temperatureDifferenceK = hotIn.TemperatureK - coldIn.TemperatureK;

        double baseEffectiveness;
        double ntu = 0.0;
        if (_modelType == HeatRecoveryModelType.CounterFlowNtu)
        {
            ntu = cMin > 0.0 ? _uaWPerK / cMin : 0.0;
            baseEffectiveness = CalculateCounterFlowEffectiveness(ntu, capacityRatio);
        }
        else
        {
            baseEffectiveness = _effectivenessFraction;
        }

        // Bypass reduces the exchanged fraction; MVP applies ε_eff = ε (1 - b) to full streams.
        var effectiveness = baseEffectiveness * (1.0 - _bypassFraction);
        LastEffectivenessFraction = effectiveness;
        LastNtu = ntu;
        LastCapacityRatio = capacityRatio;

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

        var hotVol = hotIn.DryAirMassFlowKgPerSecond * hotIn.SpecificVolumeM3PerKgDryAir;
        var coldVol = coldIn.DryAirMassFlowKgPerSecond * coldIn.SpecificVolumeM3PerKgDryAir;
        LastHotPressureDropPa = _hotReferencePressureDropPa <= 0.0
            ? 0.0
            : _hotReferencePressureDropPa
              * Math.Pow(Math.Abs(hotVol / _hotReferenceVolumetricFlowM3PerSecond), _pressureDropExponent);
        LastColdPressureDropPa = _coldReferencePressureDropPa <= 0.0
            ? 0.0
            : _coldReferencePressureDropPa
              * Math.Pow(Math.Abs(coldVol / _coldReferenceVolumetricFlowM3PerSecond), _pressureDropExponent);

        var hotOutPressure = hotIn.PressurePa - LastHotPressureDropPa;
        var coldOutPressure = coldIn.PressurePa - LastColdPressureDropPa;
        if (hotOutPressure <= 0.0 || coldOutPressure <= 0.0)
        {
            return Error(context, "HEAT_RECOVERY.NON_POSITIVE_PRESSURE", "Heat-recovery outlet pressure would be non-positive.");
        }

        var hotOut = _calculator.CreateFromHumidityRatio(
            hotOutTemperatureK,
            hotOutPressure,
            hotIn.HumidityRatioKgPerKgDryAir,
            hotIn.DryAirMassFlowKgPerSecond);

        var coldOut = _calculator.CreateFromHumidityRatio(
            coldOutTemperatureK,
            coldOutPressure,
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

    /// <summary>
    /// Counter-flow effectiveness for given NTU and capacity ratio Cr = Cmin/Cmax.
    /// </summary>
    public static double CalculateCounterFlowEffectiveness(double ntu, double capacityRatio)
    {
        FiniteNumber.RequireNonNegative(ntu, nameof(ntu));
        FiniteNumber.Require(capacityRatio, nameof(capacityRatio));
        if (capacityRatio is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacityRatio), "Capacity ratio must be in [0, 1].");
        }

        if (ntu == 0.0)
        {
            return 0.0;
        }

        if (Math.Abs(capacityRatio - 1.0) < 1e-9)
        {
            return ntu / (1.0 + ntu);
        }

        var exponent = -ntu * (1.0 - capacityRatio);
        var expTerm = Math.Exp(exponent);
        return (1.0 - expTerm) / (1.0 - capacityRatio * expTerm);
    }

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
