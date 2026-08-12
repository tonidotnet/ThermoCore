using ThermoCore.AWG.Simulation;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Components.VaporCompression;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Cooling;

/// <summary>
/// AWG cooling-plant adapter for map-based vapor compression (R5-002 / COOL-006 / COOL-007).
/// </summary>
public sealed class VaporCompressionCoolingPlantAdapter : ICoolingPlantModel
{
    private readonly VaporCompressionCoolingPlantModel _plant;
    private readonly AwgCondenserParameters _condenserDefaults;

    public VaporCompressionCoolingPlantAdapter(
        VaporCompressionPerformanceMap map,
        AwgCondenserParameters? condenserParameters = null,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentNullException.ThrowIfNull(map);
        _condenserDefaults = (condenserParameters ?? new AwgCondenserParameters
        {
            BypassFactor = 0.1,
            DrainageEfficiency = 0.95,
            FallbackSurfaceTemperatureK = 0.5 * (map.Validity.MinimumEvaporatingTemperatureK
                + map.Validity.MaximumEvaporatingTemperatureK),
            FallbackAvailableCoolingPowerW = 0.0
        }).Validate();

        _plant = new VaporCompressionCoolingPlantModel(
            map,
            bypassFactor: _condenserDefaults.BypassFactor,
            drainageEfficiency: _condenserDefaults.DrainageEfficiency,
            maximumRetainedFilmKg: _condenserDefaults.MaximumRetainedFilmKg,
            calculator: calculator);
    }

    public CoolingTechnology Technology => CoolingTechnology.VaporCompression;

    public VaporCompressionPerformanceMap Map => _plant.Map;

    public bool CompressorIsOn => _plant.CompressorIsOn;

    public void Reset() => _plant.Reset();

    public CoolingPlantResult Evaluate(CoolingPlantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Inlet);
        ArgumentNullException.ThrowIfNull(request.Simulation);

        var tevap = request.ColdSurfaceTemperatureK
            ?? request.EvaporatingTemperatureK
            ?? _condenserDefaults.FallbackSurfaceTemperatureK;
        FiniteNumber.RequirePositive(tevap, nameof(request.ColdSurfaceTemperatureK));

        var tcond = request.CondensingTemperatureK
            ?? Math.Max(tevap + 15.0, request.Inlet.TemperatureK + 10.0);
        FiniteNumber.RequirePositive(tcond, nameof(request.CondensingTemperatureK));

        var speed = request.CompressorSpeedFraction
            ?? (request.ElectricalPowerW is > 0.0 || request.AvailableCoolingPowerW is > 0.0
                || request.CompressorRequested == true
                    ? 1.0
                    : 0.0);
        FiniteNumber.Require(speed, nameof(request.CompressorSpeedFraction));

        var requested = request.CompressorRequested
            ?? (speed > 1e-9);

        var step = _plant.Evaluate(new VaporCompressionPlantStepRequest
        {
            Inlet = request.Inlet,
            Simulation = request.Simulation,
            EvaporatingTemperatureK = tevap,
            CondensingTemperatureK = tcond,
            SpeedFraction = Math.Clamp(speed, 0.0, 1.0),
            CompressorRequested = requested,
            DischargeTemperatureK = request.DischargeTemperatureK,
            ProcessFanElectricalPowerW = Math.Max(0.0, request.FanElectricalPowerW ?? 0.0)
        });

        return new CoolingPlantResult
        {
            Technology = Technology,
            Outlet = step.Outlet,
            CollectedWaterKgPerSecond = step.CollectedWaterKgPerSecond,
            CoolingDeliveredW = step.CoolingDeliveredW,
            ElectricalInputW = step.ElectricalInputW,
            ThermalInputW = step.CoolingDeliveredW,
            RejectedHeatW = step.RejectedHeatW,
            BareDeviceCop = step.BareDeviceCop
                ?? AwgCoolingMetricsCalculator.BareCoolingDeviceCop(
                    step.DeviceCoolingCapacityW,
                    step.ElectricalInputW),
            CoolingPlantCop = step.PlantCop
                ?? AwgPerformanceKpiCalculator.RatioOrNull(
                    step.CoolingDeliveredW,
                    step.ElectricalInputW + step.ProcessFanElectricalPowerW),
            PressureDropPa = 0.0,
            Balance = step.Balance,
            Diagnostics = step.Diagnostics,
            LiquidOut = step.LiquidOut,
            DeviceCoolingCapacityW = step.DeviceCoolingCapacityW,
            TechnologySpecificValues = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["evaporatingTemperatureK"] = tevap,
                ["condensingTemperatureK"] = tcond,
                ["speedFraction"] = Math.Clamp(speed, 0.0, 1.0),
                ["compressorOn"] = step.CompressorOn ? 1.0 : 0.0,
                ["heldByCycling"] = step.HeldByCyclingLimits ? 1.0 : 0.0,
                ["outsideValidity"] = step.OutsideValidity ? 1.0 : 0.0,
                ["mapRejected"] = step.Rejected ? 1.0 : 0.0
            }
        };
    }
}
