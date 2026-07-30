using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Polynomial fan performance curve with affinity-law control fraction
/// (AIR-004 / docs/03_Components/13_FanAndAirflow.md §7).
/// Δp(V̇,u) = (a0 + a1·V̇' + a2·V̇'²)·u² where V̇' = V̇/u for u&gt;0.
/// When <see cref="SolveAgainstSystemCurve"/> is true, flow is the fan/system intersection (AIR-006).
/// </summary>
public sealed class CurveBasedFanComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _a0Pa;
    private readonly double _a1PaPerM3s;
    private readonly double _a2PaPerM3s2;
    private readonly double _controlFraction;
    private readonly double _fanEfficiency;
    private readonly double _driverEfficiency;
    private readonly Func<double, double>? _systemPressureDropPa;
    private readonly bool _solveAgainstSystemCurve;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public CurveBasedFanComponent(
        string id,
        double shutoffPressureRisePa,
        double linearCoefficientPaPerM3s,
        double quadraticCoefficientPaPerM3s2,
        double controlFraction = 1.0,
        double fanEfficiency = 0.60,
        double driverEfficiency = 0.90,
        bool solveAgainstSystemCurve = false,
        Func<double, double>? systemPressureDropPa = null,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequirePositive(shutoffPressureRisePa, nameof(shutoffPressureRisePa));
        FiniteNumber.Require(linearCoefficientPaPerM3s, nameof(linearCoefficientPaPerM3s));
        FiniteNumber.Require(quadraticCoefficientPaPerM3s2, nameof(quadraticCoefficientPaPerM3s2));
        FiniteNumber.Require(controlFraction, nameof(controlFraction));
        if (controlFraction is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(controlFraction));
        }

        FiniteNumber.RequirePositive(fanEfficiency, nameof(fanEfficiency));
        FiniteNumber.RequirePositive(driverEfficiency, nameof(driverEfficiency));
        if (fanEfficiency > 1.0 || driverEfficiency > 1.0)
        {
            throw new ArgumentOutOfRangeException("Efficiencies must be in (0, 1].");
        }

        if (solveAgainstSystemCurve && systemPressureDropPa is null)
        {
            throw new ArgumentException("System pressure-drop function is required when solving the operating point.");
        }

        Id = id;
        _a0Pa = shutoffPressureRisePa;
        _a1PaPerM3s = linearCoefficientPaPerM3s;
        _a2PaPerM3s2 = quadraticCoefficientPaPerM3s2;
        _controlFraction = controlFraction;
        _fanEfficiency = fanEfficiency;
        _driverEfficiency = driverEfficiency;
        _solveAgainstSystemCurve = solveAgainstSystemCurve;
        _systemPressureDropPa = systemPressureDropPa;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastElectricalPowerW { get; private set; }

    public double LastAirPowerW { get; private set; }

    public double LastPressureRisePa { get; private set; }

    public double LastVolumetricFlowM3PerSecond { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastElectricalPowerW = 0.0;
        LastAirPowerW = 0.0;
        LastPressureRisePa = 0.0;
        LastVolumetricFlowM3PerSecond = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.MISSING_INLET",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Fan '{Id}' requires MoistAirState on 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet"
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        var diagnostics = new List<SimulationDiagnostic>();
        double volumetricFlow;
        double pressureRise;

        if (_solveAgainstSystemCurve)
        {
            var solved = FanSystemOperatingPointSolver.TrySolve(
                FanPressureRisePa,
                _systemPressureDropPa!,
                volumetricFlowGuessM3PerSecond: Math.Max(1e-4, inlet.DryAirMassFlowKgPerSecond * inlet.SpecificVolumeM3PerKgDryAir),
                out volumetricFlow,
                out pressureRise);
            if (!solved)
            {
                return new ComponentStepResult
                {
                    Diagnostics =
                    [
                        new SimulationDiagnostic
                        {
                            Code = "FAN.NO_OPERATING_POINT",
                            Severity = DiagnosticSeverity.Error,
                            Message = "Fan curve and system curve do not intersect in the search range.",
                            ComponentId = Id
                        }
                    ],
                    Balance = ConservationBalance.Empty
                };
            }
        }
        else
        {
            volumetricFlow = inlet.DryAirMassFlowKgPerSecond * inlet.SpecificVolumeM3PerKgDryAir;
            pressureRise = FanPressureRisePa(volumetricFlow);
            if (pressureRise < 0.0)
            {
                diagnostics.Add(new SimulationDiagnostic
                {
                    Code = "FAN.OUTSIDE_CURVE",
                    Severity = DiagnosticSeverity.Warning,
                    Message = "Requested flow is beyond the fan free-delivery point; pressure rise clamped to zero.",
                    ComponentId = Id
                });
                pressureRise = 0.0;
            }
        }

        LastVolumetricFlowM3PerSecond = volumetricFlow;
        LastPressureRisePa = pressureRise;
        var dryAirFlow = volumetricFlow / Math.Max(inlet.SpecificVolumeM3PerKgDryAir, 1e-12);
        var outlet = _calculator.CreateFromHumidityRatio(
            inlet.TemperatureK,
            inlet.PressurePa + pressureRise,
            inlet.HumidityRatioKgPerKgDryAir,
            dryAirFlow);

        LastAirPowerW = pressureRise * volumetricFlow;
        LastElectricalPowerW = _controlFraction <= 0.0
            ? 0.0
            : LastAirPowerW / (_fanEfficiency * _driverEfficiency);

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: dryAirFlow,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: dryAirFlow * inlet.HumidityRatioKgPerKgDryAir,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep,
            electricalPowerInputW: LastElectricalPowerW,
            electricalPowerOutputW: LastElectricalPowerW);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = outlet
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

    public double FanPressureRisePa(double volumetricFlowM3PerSecond)
    {
        FiniteNumber.RequireNonNegative(volumetricFlowM3PerSecond, nameof(volumetricFlowM3PerSecond));
        if (_controlFraction <= 0.0)
        {
            return 0.0;
        }

        // Affinity: map actual flow to full-speed equivalent.
        var vPrime = volumetricFlowM3PerSecond / _controlFraction;
        var deltaFull = _a0Pa + _a1PaPerM3s * vPrime + _a2PaPerM3s2 * vPrime * vPrime;
        return deltaFull * _controlFraction * _controlFraction;
    }
}
