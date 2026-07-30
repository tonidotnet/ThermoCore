using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components;

/// <summary>
/// Passive duct with reference-curve pressure loss; moist-air state otherwise unchanged
/// except outlet pressure is reduced by Δp (docs/03_Components/13_FanAndAirflow.md).
/// </summary>
public sealed class DuctPressureLossComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _pressureDropRefPa;
    private readonly double _volumetricFlowRefM3PerSecond;
    private readonly double _exponent;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public DuctPressureLossComponent(
        string id,
        double pressureDropRefPa,
        double volumetricFlowRefM3PerSecond,
        double exponent = 2.0,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequireNonNegative(pressureDropRefPa, nameof(pressureDropRefPa));
        FiniteNumber.RequirePositive(volumetricFlowRefM3PerSecond, nameof(volumetricFlowRefM3PerSecond));
        FiniteNumber.RequirePositive(exponent, nameof(exponent));

        Id = id;
        _pressureDropRefPa = pressureDropRefPa;
        _volumetricFlowRefM3PerSecond = volumetricFlowRefM3PerSecond;
        _exponent = exponent;
        _calculator = calculator ?? new PsychrometricCalculator();
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public double LastPressureDropPa { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastPressureDropPa = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return MissingInlet(context);
        }

        var volumetricFlow = inlet.DryAirMassFlowKgPerSecond * inlet.SpecificVolumeM3PerKgDryAir;
        var ratio = volumetricFlow / _volumetricFlowRefM3PerSecond;
        LastPressureDropPa = _pressureDropRefPa * Math.Pow(Math.Abs(ratio), _exponent);

        var outletPressure = inlet.PressurePa - LastPressureDropPa;
        if (outletPressure <= 0.0)
        {
            return new ComponentStepResult
            {
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "DUCT.NON_POSITIVE_PRESSURE",
                        Severity = DiagnosticSeverity.Error,
                        Message = $"Duct '{Id}' outlet pressure would be non-positive.",
                        ComponentId = Id,
                        Values = new Dictionary<string, double>(StringComparer.Ordinal)
                        {
                            ["inletPressurePa"] = inlet.PressurePa,
                            ["pressureDropPa"] = LastPressureDropPa
                        }
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        var outlet = _calculator.CreateFromHumidityRatio(
            inlet.TemperatureK,
            outletPressure,
            inlet.HumidityRatioKgPerKgDryAir,
            inlet.DryAirMassFlowKgPerSecond);

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir,
            energyOutputW: outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = outlet
            },
            Balance = balance
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private ComponentStepResult MissingInlet(ComponentStepContext context)
        => new()
        {
            Diagnostics =
            [
                new SimulationDiagnostic
                {
                    Code = "COMPONENT.MISSING_INLET",
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Duct '{Id}' requires MoistAirState on 'inlet'.",
                    ComponentId = Id,
                    PortId = "inlet",
                    StepIndex = context.Simulation.StepIndex
                }
            ],
            Balance = ConservationBalance.Empty
        };
}

/// <summary>
/// Prescribed-flow fan: forces dry-air mass flow, applies pressure rise, reports electrical power
/// (docs/03_Components/13_FanAndAirflow.md §8–§9).
/// </summary>
public sealed class PrescribedFlowFanComponent : ISimulationComponent
{
    private readonly IPsychrometricCalculator _calculator;
    private readonly double _dryAirMassFlowKgPerSecond;
    private readonly double _pressureRisePa;
    private readonly double _fanEfficiency;
    private readonly double _driverEfficiency;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public PrescribedFlowFanComponent(
        string id,
        double dryAirMassFlowKgPerSecond,
        double pressureRisePa,
        double fanEfficiency = 0.60,
        double driverEfficiency = 0.90,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        FiniteNumber.RequirePositive(dryAirMassFlowKgPerSecond, nameof(dryAirMassFlowKgPerSecond));
        FiniteNumber.RequireNonNegative(pressureRisePa, nameof(pressureRisePa));
        FiniteNumber.RequirePositive(fanEfficiency, nameof(fanEfficiency));
        FiniteNumber.RequirePositive(driverEfficiency, nameof(driverEfficiency));
        if (fanEfficiency > 1.0 || driverEfficiency > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(fanEfficiency), "Efficiencies must be in (0, 1].");
        }

        Id = id;
        _dryAirMassFlowKgPerSecond = dryAirMassFlowKgPerSecond;
        _pressureRisePa = pressureRisePa;
        _fanEfficiency = fanEfficiency;
        _driverEfficiency = driverEfficiency;
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

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastElectricalPowerW = 0.0;
        LastAirPowerW = 0.0;
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
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex
                    }
                ],
                Balance = ConservationBalance.Empty
            };
        }

        var outlet = _calculator.CreateFromHumidityRatio(
            inlet.TemperatureK,
            inlet.PressurePa + _pressureRisePa,
            inlet.HumidityRatioKgPerKgDryAir,
            _dryAirMassFlowKgPerSecond);

        var volumetricFlow = outlet.DryAirMassFlowKgPerSecond * outlet.SpecificVolumeM3PerKgDryAir;
        LastAirPowerW = _pressureRisePa * volumetricFlow;
        LastElectricalPowerW = LastAirPowerW / (_fanEfficiency * _driverEfficiency);

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: _dryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: _dryAirMassFlowKgPerSecond * inlet.HumidityRatioKgPerKgDryAir,
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
            Balance = balance
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;
}

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

/// <summary>
/// Finds fan/system curve intersection Δp_fan(V̇)=Δp_sys(V̇) (AIR-005/AIR-006).
/// </summary>
public static class FanSystemOperatingPointSolver
{
    public static bool TrySolve(
        Func<double, double> fanPressureRisePa,
        Func<double, double> systemPressureDropPa,
        double volumetricFlowGuessM3PerSecond,
        out double volumetricFlowM3PerSecond,
        out double pressurePa,
        double maxVolumetricFlowM3PerSecond = 5.0,
        int maxIterations = 80)
    {
        ArgumentNullException.ThrowIfNull(fanPressureRisePa);
        ArgumentNullException.ThrowIfNull(systemPressureDropPa);
        FiniteNumber.RequirePositive(volumetricFlowGuessM3PerSecond, nameof(volumetricFlowGuessM3PerSecond));
        FiniteNumber.RequirePositive(maxVolumetricFlowM3PerSecond, nameof(maxVolumetricFlowM3PerSecond));

        double Residual(double v) => fanPressureRisePa(v) - systemPressureDropPa(v);

        var lo = 0.0;
        var hi = maxVolumetricFlowM3PerSecond;
        var rLo = Residual(lo);
        var rHi = Residual(hi);
        if (rLo == 0.0)
        {
            volumetricFlowM3PerSecond = lo;
            pressurePa = systemPressureDropPa(lo);
            return true;
        }

        if (rLo * rHi > 0.0)
        {
            // Expand hi a few times from the guess.
            hi = Math.Max(volumetricFlowGuessM3PerSecond, 1e-3);
            rHi = Residual(hi);
            for (var i = 0; i < 20 && rLo * rHi > 0.0; i++)
            {
                hi *= 1.5;
                if (hi > maxVolumetricFlowM3PerSecond * 4.0)
                {
                    break;
                }

                rHi = Residual(hi);
            }

            if (rLo * rHi > 0.0)
            {
                volumetricFlowM3PerSecond = 0.0;
                pressurePa = 0.0;
                return false;
            }
        }

        for (var i = 0; i < maxIterations; i++)
        {
            var mid = 0.5 * (lo + hi);
            var rMid = Residual(mid);
            if (Math.Abs(rMid) < 1e-4 || Math.Abs(hi - lo) < 1e-8)
            {
                volumetricFlowM3PerSecond = mid;
                pressurePa = systemPressureDropPa(mid);
                return pressurePa >= 0.0 && fanPressureRisePa(mid) >= 0.0;
            }

            if (rLo * rMid <= 0.0)
            {
                hi = mid;
                rHi = rMid;
            }
            else
            {
                lo = mid;
                rLo = rMid;
            }
        }

        volumetricFlowM3PerSecond = 0.5 * (lo + hi);
        pressurePa = systemPressureDropPa(volumetricFlowM3PerSecond);
        return true;
    }
}
