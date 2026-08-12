using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// Simulation component wrapping <see cref="CommercialPeltierDehumidifierModel"/>
/// (moist-air in/out + condensate + optional electrical). Not wired into AWG V3 (R4).
/// </summary>
public sealed class CommercialPeltierDehumidifierComponent : ISimulationComponent
{
    private readonly CommercialPeltierDehumidifierModel _model;
    private readonly List<SimulationDiagnostic> _diagnostics = [];

    public CommercialPeltierDehumidifierComponent(
        string id,
        CommercialPeltierDehumidifierProfile profile,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(profile);
        Id = id;
        _model = new CommercialPeltierDehumidifierModel(profile, calculator);
        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir),
            new PhysicalPort("liquid_out", id, PortDirection.Output, PhysicalDomain.LiquidWater),
            new PhysicalPort("electrical", id, PortDirection.Input, PhysicalDomain.Electricity, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public CommercialPeltierDehumidifierProfile Profile => _model.Profile;

    public double LastWaterProductionRateKgPerSecond { get; private set; }

    public double LastElectricalPowerW { get; private set; }

    public double LastDeliveredCoolingPowerW { get; private set; }

    public bool LastOutsideValidity { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastWaterProductionRateKgPerSecond = 0.0;
        LastElectricalPowerW = 0.0;
        LastDeliveredCoolingPowerW = 0.0;
        LastOutsideValidity = false;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();

        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return new ComponentStepResult
            {
                OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal),
                Balance = ConservationBalance.Empty,
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.MISSING_INLET",
                        Severity = DiagnosticSeverity.Error,
                        Message = "Commercial Peltier dehumidifier requires MoistAirState on 'inlet'.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex
                    }
                ]
            };
        }

        if (inlet.DryAirMassFlowKgPerSecond <= 0.0)
        {
            return new ComponentStepResult
            {
                OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal),
                Balance = ConservationBalance.Empty,
                Diagnostics =
                [
                    new SimulationDiagnostic
                    {
                        Code = "COMPONENT.ZERO_FLOW",
                        Severity = DiagnosticSeverity.Error,
                        Message = "Commercial Peltier dehumidifier requires positive dry-air mass flow.",
                        ComponentId = Id,
                        PortId = "inlet",
                        StepIndex = context.Simulation.StepIndex
                    }
                ]
            };
        }

        double? electricalOverride = null;
        if (context.InputStates.TryGetValue("electrical", out var elecRaw)
            && elecRaw is ElectricalPowerState electrical)
        {
            FiniteNumber.RequireNonNegative(electrical.PowerW, nameof(electrical.PowerW));
            electricalOverride = electrical.PowerW;
        }

        var result = _model.Evaluate(inlet, electricalOverride);
        diagnostics.AddRange(result.Diagnostics.Select(d => d with
        {
            ComponentId = Id,
            StepIndex = context.Simulation.StepIndex
        }));

        LastWaterProductionRateKgPerSecond = result.WaterProductionRateKgPerSecond;
        LastElectricalPowerW = result.ElectricalPowerW;
        LastDeliveredCoolingPowerW = result.DeliveredCoolingPowerW;
        LastOutsideValidity = result.OutsideValidity;

        var liquidTemperatureK = result.ColdSurfaceTemperatureK
            ?? Math.Min(result.Outlet.TemperatureK, inlet.TemperatureK);

        // Air-side cooling is enthalpy drop; electrical input is rejected with that cooling
        // (liquid enthalpy neglected in MVP bookkeeping, matching CondenserComponent).
        var rejectedHeatW = result.DeliveredCoolingPowerW + result.ElectricalPowerW;
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: result.Outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: result.Outlet.WaterVaporMassFlowKgPerSecond
                + result.WaterProductionRateKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir
                + result.ElectricalPowerW,
            energyOutputW: result.Outlet.DryAirMassFlowKgPerSecond * result.Outlet.SpecificEnthalpyJPerKgDryAir
                + rejectedHeatW,
            storedEnergyChangeW: 0.0,
            timeStep: context.Simulation.TimeStep,
            electricalPowerInputW: result.ElectricalPowerW,
            electricalPowerOutputW: result.ElectricalPowerW);

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = result.Outlet,
                ["liquid_out"] = new LiquidWaterState
                {
                    MassFlowKgPerSecond = result.WaterProductionRateKgPerSecond,
                    TemperatureK = liquidTemperatureK
                }
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
}
