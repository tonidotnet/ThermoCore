using ThermoCore.AWG.Simulation;
using ThermoCore.Core.Balances;
using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Cooling;

/// <summary>
/// AWG adapter over Core <see cref="CommercialPeltierDehumidifierModel"/> (COOL-005 / R4-001).
/// Does not rewrite the AWG V3 graph; used for plant-level comparison.
/// </summary>
public sealed class CommercialPeltierCoolingPlantAdapter : ICoolingPlantModel
{
    private readonly CommercialPeltierDehumidifierModel _model;

    public CommercialPeltierCoolingPlantAdapter(
        CommercialPeltierDehumidifierProfile profile,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _model = new CommercialPeltierDehumidifierModel(profile, calculator);
    }

    public CoolingTechnology Technology => CoolingTechnology.CommercialPeltierDehumidifier;

    public CommercialPeltierDehumidifierProfile Profile => _model.Profile;

    public CoolingPlantResult Evaluate(CoolingPlantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Inlet);
        ArgumentNullException.ThrowIfNull(request.Simulation);

        if (request.ElectricalPowerW is { } pe)
        {
            FiniteNumber.RequireNonNegative(pe, nameof(request.ElectricalPowerW));
        }

        var evaluated = _model.Evaluate(request.Inlet, request.ElectricalPowerW);
        var fanW = Math.Max(0.0, request.FanElectricalPowerW ?? 0.0);
        var rejectedW = evaluated.DeliveredCoolingPowerW + evaluated.ElectricalPowerW;

        var liquid = new LiquidWaterState
        {
            MassFlowKgPerSecond = evaluated.WaterProductionRateKgPerSecond,
            TemperatureK = evaluated.ColdSurfaceTemperatureK
                ?? Math.Min(evaluated.Outlet.TemperatureK, request.Inlet.TemperatureK)
        };

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: request.Inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: evaluated.Outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: request.Inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: evaluated.Outlet.WaterVaporMassFlowKgPerSecond
                + evaluated.WaterProductionRateKgPerSecond,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: request.Inlet.DryAirMassFlowKgPerSecond * request.Inlet.SpecificEnthalpyJPerKgDryAir
                + evaluated.ElectricalPowerW,
            energyOutputW: evaluated.Outlet.DryAirMassFlowKgPerSecond * evaluated.Outlet.SpecificEnthalpyJPerKgDryAir
                + rejectedW,
            storedEnergyChangeW: 0.0,
            timeStep: request.Simulation.TimeStep,
            electricalPowerInputW: evaluated.ElectricalPowerW,
            electricalPowerOutputW: evaluated.ElectricalPowerW);

        var tech = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["outsideValidity"] = evaluated.OutsideValidity ? 1.0 : 0.0,
            ["outletStateFromMap"] = evaluated.OutletStateFromMap ? 1.0 : 0.0
        };
        if (evaluated.ColdSurfaceTemperatureK is { } tc)
        {
            tech["coldSurfaceTemperatureK"] = tc;
        }

        if (evaluated.HotSideTemperatureK is { } th)
        {
            tech["hotSideTemperatureK"] = th;
        }

        return new CoolingPlantResult
        {
            Technology = Technology,
            Outlet = evaluated.Outlet,
            CollectedWaterKgPerSecond = evaluated.WaterProductionRateKgPerSecond,
            CoolingDeliveredW = evaluated.DeliveredCoolingPowerW,
            ElectricalInputW = evaluated.ElectricalPowerW,
            ThermalInputW = evaluated.DeliveredCoolingPowerW,
            RejectedHeatW = rejectedW,
            BareDeviceCop = CommercialPeltierBlackBoxKpis.BareCoolingDeviceCop(
                evaluated.DeliveredCoolingPowerW,
                evaluated.ElectricalPowerW),
            CoolingPlantCop = AwgPerformanceKpiCalculator.RatioOrNull(
                evaluated.DeliveredCoolingPowerW,
                evaluated.ElectricalPowerW + fanW),
            PressureDropPa = 0.0,
            Balance = balance,
            Diagnostics = evaluated.Diagnostics,
            LiquidOut = liquid,
            DeviceCoolingCapacityW = evaluated.DeliveredCoolingPowerW,
            TechnologySpecificValues = tech
        };
    }
}
