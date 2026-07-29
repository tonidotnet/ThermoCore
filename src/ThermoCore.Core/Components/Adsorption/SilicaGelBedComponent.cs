using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Physics;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

/// <summary>
/// LDF silica-gel bed with configurable isotherm, capacity limits, and lumped thermal mass
/// (docs/03_Components/09_SilicaGel.md fidelity level 2).
/// </summary>
public sealed class SilicaGelBedComponent : ISimulationComponent
{
    private readonly SilicaGelParameters _parameters;
    private readonly ISilicaGelIsotherm _isotherm;
    private readonly IPsychrometricCalculator _calculator;
    private readonly List<SimulationDiagnostic> _diagnostics = [];
    private SilicaGelState _state;

    public SilicaGelBedComponent(
        string id,
        SilicaGelParameters parameters,
        ISilicaGelIsotherm isotherm,
        SilicaGelState? initialState = null,
        IPsychrometricCalculator? calculator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(isotherm);

        Id = id;
        _parameters = parameters.Validate();
        _isotherm = isotherm;
        _calculator = calculator ?? new PsychrometricCalculator();
        _state = initialState ?? SilicaGelState.Create(
            dryAdsorbentMassKg: _parameters.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            bedTemperatureK: _parameters.AmbientTemperatureK,
            maximumWaterLoadingKgPerKgDryAdsorbent: _parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: _parameters.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: _parameters.BedHousingThermalCapacityJPerK);

        if (Math.Abs(_state.DryAdsorbentMassKg - _parameters.DryAdsorbentMassKg) > 1e-12)
        {
            throw new ArgumentException("Initial state dry mass must match parameters.", nameof(initialState));
        }

        Ports =
        [
            new PhysicalPort("inlet", id, PortDirection.Input, PhysicalDomain.MoistAir),
            new PhysicalPort("outlet", id, PortDirection.Output, PhysicalDomain.MoistAir),
            new PhysicalPort("external_heat", id, PortDirection.Input, PhysicalDomain.Heat, isRequired: false)
        ];
    }

    public string Id { get; }

    public IReadOnlyList<IPhysicalPort> Ports { get; }

    public SilicaGelState State => _state;

    public double LastWaterTransferRateKgPerSecond { get; private set; }

    public double LastAdsorptionHeatW { get; private set; }

    public double LastEquilibriumLoadingKgPerKg { get; private set; }

    public double LastAvailableDesorptionHeatW { get; private set; }

    public bool LastDesorptionWasEnergyLimited { get; private set; }

    public double LastPressureDropPa { get; private set; }

    public void Initialize(SimulationContext context)
    {
        _diagnostics.Clear();
        LastWaterTransferRateKgPerSecond = 0.0;
        LastAdsorptionHeatW = 0.0;
        LastEquilibriumLoadingKgPerKg = _state.EquilibriumLoadingKgPerKgDryAdsorbent;
        LastAvailableDesorptionHeatW = 0.0;
        LastDesorptionWasEnergyLimited = false;
        LastPressureDropPa = 0.0;
    }

    public ComponentStepResult Evaluate(ComponentStepContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var diagnostics = new List<SimulationDiagnostic>();
        var dt = context.Simulation.TimeStep.TotalSeconds;
        if (dt <= 0.0)
        {
            return Error(context, "COMPONENT.INVALID_TIMESTEP", "Silica-gel bed requires a positive timestep.");
        }

        if (!context.InputStates.TryGetValue("inlet", out var raw) || raw is not MoistAirState inlet)
        {
            return Error(context, "COMPONENT.MISSING_INLET", "Silica-gel bed requires MoistAirState on 'inlet'.", "inlet");
        }

        if (inlet.DryAirMassFlowKgPerSecond <= 0.0)
        {
            return Error(context, "COMPONENT.ZERO_FLOW", "Silica-gel bed requires positive dry-air mass flow.", "inlet");
        }

        var externalHeatW = 0.0;
        if (context.InputStates.TryGetValue("external_heat", out var heatRaw)
            && heatRaw is HeatFlowState heat)
        {
            FiniteNumber.Require(heat.HeatFlowW, nameof(heat.HeatFlowW));
            externalHeatW = heat.HeatFlowW;
        }

        var stateBedTemperatureK = _state.BedTemperatureK;
        var loading = _state.WaterLoadingKgPerKgDryAdsorbent;
        var bedTemperatureGuessK = stateBedTemperatureK;
        var tolerances = context.Simulation.NumericalTolerances;
        var maxIterations = Math.Max(8, Math.Min(tolerances.MaximumIterations, 30));

        double equilibriumLoading = 0.0;
        double waterTransferRate = 0.0;
        double adsorptionHeatW = 0.0;
        double proposedLoading = loading;
        double proposedBedTemperatureK = stateBedTemperatureK;
        MoistAirState? outlet = null;
        var limitedByVapor = false;
        var limitedByCapacity = false;
        var limitedByEnergy = false;
        var availableDesorptionHeatW = 0.0;
        var minVaporPressurePa = Math.Max(
            12.0,
            _calculator.CalculateSaturationPressurePa(230.0));

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var saturationAtBed = _calculator.CalculateSaturationPressurePa(bedTemperatureGuessK);
            equilibriumLoading = _isotherm.CalculateEquilibriumLoadingKgPerKg(
                bedTemperatureGuessK,
                inlet.VaporPressurePa,
                saturationAtBed);

            var kineticCoefficient = CalculateKineticCoefficient(bedTemperatureGuessK);
            var unconstrainedLoading = equilibriumLoading
                + (loading - equilibriumLoading) * Math.Exp(-kineticCoefficient * dt);

            proposedLoading = Math.Clamp(
                unconstrainedLoading,
                _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
                _parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);

            limitedByCapacity = Math.Abs(proposedLoading - unconstrainedLoading) > 1e-12;
            limitedByVapor = false;
            limitedByEnergy = false;

            var deltaLoading = proposedLoading - loading;
            var deltaWaterKg = _parameters.DryAdsorbentMassKg * deltaLoading;

            // Adsorption cannot remove more vapor than available in the timestep.
            if (deltaWaterKg > 0.0)
            {
                var availableVaporKg = inlet.WaterVaporMassFlowKgPerSecond * dt;
                if (deltaWaterKg > availableVaporKg)
                {
                    deltaWaterKg = Math.Max(0.0, availableVaporKg);
                    proposedLoading = loading + deltaWaterKg / _parameters.DryAdsorbentMassKg;
                    limitedByVapor = true;
                }
            }
            else if (deltaWaterKg < 0.0)
            {
                // Desorption cannot release more water than stored above the regenerated floor.
                var releasableKg = (loading - _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent)
                    * _parameters.DryAdsorbentMassKg;
                if (-deltaWaterKg > releasableKg)
                {
                    deltaWaterKg = -Math.Max(0.0, releasableKg);
                    proposedLoading = loading + deltaWaterKg / _parameters.DryAdsorbentMassKg;
                    limitedByCapacity = true;
                }

                if (_parameters.EnableEnergyLimitedDesorption && deltaWaterKg < 0.0)
                {
                    availableDesorptionHeatW = EstimateAvailableDesorptionHeatW(
                        inlet,
                        bedTemperatureGuessK,
                        externalHeatW,
                        dt);

                    var maxDesorptionRateKgPerSecond = availableDesorptionHeatW
                        / _parameters.EffectiveHeatOfAdsorptionJPerKgWater;
                    var desorptionRateKgPerSecond = -deltaWaterKg / dt;
                    if (desorptionRateKgPerSecond > maxDesorptionRateKgPerSecond)
                    {
                        deltaWaterKg = -maxDesorptionRateKgPerSecond * dt;
                        proposedLoading = loading + deltaWaterKg / _parameters.DryAdsorbentMassKg;
                        limitedByEnergy = true;
                    }
                }
            }

            waterTransferRate = deltaWaterKg / dt;
            adsorptionHeatW = _parameters.EffectiveHeatOfAdsorptionJPerKgWater * waterTransferRate;

            var vaporOutKgPerSecond = inlet.WaterVaporMassFlowKgPerSecond - waterTransferRate;
            if (vaporOutKgPerSecond < 0.0)
            {
                vaporOutKgPerSecond = 0.0;
                waterTransferRate = inlet.WaterVaporMassFlowKgPerSecond;
                deltaWaterKg = waterTransferRate * dt;
                proposedLoading = loading + deltaWaterKg / _parameters.DryAdsorbentMassKg;
                adsorptionHeatW = _parameters.EffectiveHeatOfAdsorptionJPerKgWater * waterTransferRate;
                limitedByVapor = true;
            }

            var humidityOut = vaporOutKgPerSecond / inlet.DryAirMassFlowKgPerSecond;
            var minHumidity = _calculator.CalculateHumidityRatio(inlet.PressurePa, minVaporPressurePa);
            if (humidityOut < minHumidity)
            {
                humidityOut = minHumidity;
                vaporOutKgPerSecond = humidityOut * inlet.DryAirMassFlowKgPerSecond;
                waterTransferRate = inlet.WaterVaporMassFlowKgPerSecond - vaporOutKgPerSecond;
                proposedLoading = Math.Clamp(
                    loading + waterTransferRate * dt / _parameters.DryAdsorbentMassKg,
                    _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
                    _parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
                adsorptionHeatW = _parameters.EffectiveHeatOfAdsorptionJPerKgWater * waterTransferRate;
                limitedByVapor = true;
            }

            var capacityRate = inlet.DryAirMassFlowKgPerSecond
                * (ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK
                   + inlet.HumidityRatioKgPerKgDryAir * ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK);

            var effectiveness = capacityRate <= 0.0 || _parameters.AirBedHeatTransferCoefficientWPerK <= 0.0
                ? 0.0
                : 1.0 - Math.Exp(-_parameters.AirBedHeatTransferCoefficientWPerK / capacityRate);

            var outletTemperatureK = inlet.TemperatureK
                - effectiveness * (inlet.TemperatureK - bedTemperatureGuessK);
            outletTemperatureK = Math.Clamp(outletTemperatureK, 230.0, 373.0);

            // Cap humidity at saturation at the outlet temperature to keep a valid moist-air state.
            var saturationOut = _calculator.CalculateSaturationPressurePa(outletTemperatureK);
            if (saturationOut < inlet.PressurePa)
            {
                var maxHumidity = _calculator.CalculateHumidityRatio(
                    inlet.PressurePa,
                    Math.Min(saturationOut * 0.999, inlet.PressurePa * 0.99));
                if (humidityOut > maxHumidity)
                {
                    humidityOut = maxHumidity;
                    vaporOutKgPerSecond = humidityOut * inlet.DryAirMassFlowKgPerSecond;
                    waterTransferRate = inlet.WaterVaporMassFlowKgPerSecond - vaporOutKgPerSecond;
                    proposedLoading = Math.Clamp(
                        loading + waterTransferRate * dt / _parameters.DryAdsorbentMassKg,
                        _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
                        _parameters.MaximumWaterLoadingKgPerKgDryAdsorbent);
                    adsorptionHeatW = _parameters.EffectiveHeatOfAdsorptionJPerKgWater * waterTransferRate;
                    if (iteration == 0)
                    {
                        diagnostics.Add(Diagnostic(
                            context,
                            "SILICA.OUTLET_SATURATION_LIMIT",
                            DiagnosticSeverity.Warning,
                            "Outlet humidity was limited to saturation at the calculated outlet temperature."));
                    }
                }
            }

            outlet = _calculator.CreateFromHumidityRatio(
                outletTemperatureK,
                inlet.PressurePa,
                humidityOut,
                inlet.DryAirMassFlowKgPerSecond);

            // Pressure drop applied after psychrometric state at inlet pressure (SG-009).
            var pressureDropPa = CalculatePressureDropPa(inlet);
            var outletPressurePa = inlet.PressurePa - pressureDropPa;
            if (outletPressurePa <= 0.0)
            {
                return Error(
                    context,
                    "SILICA.NON_POSITIVE_PRESSURE",
                    "Silica-gel bed outlet pressure would be non-positive after pressure drop.");
            }

            if (Math.Abs(pressureDropPa) > 1e-12)
            {
                outlet = _calculator.CreateFromHumidityRatio(
                    outletTemperatureK,
                    outletPressurePa,
                    humidityOut,
                    inlet.DryAirMassFlowKgPerSecond);
            }

            LastPressureDropPa = pressureDropPa;

            var enthalpyInW = inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir;
            var enthalpyOutW = outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir;
            var heatLossW = _parameters.BedHeatLossCoefficientWPerK
                * (bedTemperatureGuessK - _parameters.AmbientTemperatureK);

            var bedCapacityJPerK = BedThermalCapacityJPerK(proposedLoading);
            var bedEnergyRateW = enthalpyInW - enthalpyOutW + externalHeatW - heatLossW + adsorptionHeatW;
            // Integrate from the committed state temperature, not the iterating guess.
            proposedBedTemperatureK = stateBedTemperatureK + bedEnergyRateW * dt / bedCapacityJPerK;
            // Keep the lumped bed within the psychrometric calculator support window.
            proposedBedTemperatureK = Math.Clamp(proposedBedTemperatureK, 230.0, 373.0);

            // Limit per-step temperature excursion for large timesteps (explicit Euler stability).
            const double maxTemperatureStepK = 15.0;
            proposedBedTemperatureK = Math.Clamp(
                proposedBedTemperatureK,
                stateBedTemperatureK - maxTemperatureStepK,
                stateBedTemperatureK + maxTemperatureStepK);

            if (Math.Abs(proposedBedTemperatureK - bedTemperatureGuessK) < tolerances.TemperatureK)
            {
                bedTemperatureGuessK = proposedBedTemperatureK;
                break;
            }

            bedTemperatureGuessK = 0.5 * (bedTemperatureGuessK + proposedBedTemperatureK);
        }

        if (outlet is null)
        {
            return Error(context, "SILICA.SOLVER_FAILED", "Silica-gel bed failed to produce an outlet state.");
        }

        if (limitedByVapor)
        {
            diagnostics.Add(Diagnostic(
                context,
                "SILICA.VAPOR_AVAILABILITY_LIMIT",
                DiagnosticSeverity.Information,
                "Water transfer was limited by available inlet vapor."));
        }

        if (limitedByCapacity)
        {
            diagnostics.Add(Diagnostic(
                context,
                "SILICA.CAPACITY_LIMIT",
                DiagnosticSeverity.Information,
                "Water transfer was limited by adsorbent capacity or regenerated floor."));
        }

        if (limitedByEnergy)
        {
            diagnostics.Add(Diagnostic(
                context,
                "SILICA.ENERGY_LIMITED_DESORPTION",
                DiagnosticSeverity.Information,
                "Desorption rate was limited by available regeneration heat."));
        }

        var drivingForce = equilibriumLoading - _state.WaterLoadingKgPerKgDryAdsorbent;
        var nearEquilibrium = Math.Abs(drivingForce) <= _parameters.NearEquilibriumLoadingToleranceKgPerKg;
        var regime = ClassifyRegime(drivingForce, proposedLoading, nearEquilibrium);

        var proposedState = SilicaGelState.Create(
            dryAdsorbentMassKg: _parameters.DryAdsorbentMassKg,
            waterLoadingKgPerKgDryAdsorbent: proposedLoading,
            bedTemperatureK: proposedBedTemperatureK,
            maximumWaterLoadingKgPerKgDryAdsorbent: _parameters.MaximumWaterLoadingKgPerKgDryAdsorbent,
            minimumRegeneratedLoadingKgPerKgDryAdsorbent: _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent,
            effectiveSpecificHeatJPerKgK: _parameters.EffectiveSpecificHeatJPerKgK,
            bedHousingThermalCapacityJPerK: _parameters.BedHousingThermalCapacityJPerK,
            equilibriumLoadingKgPerKgDryAdsorbent: equilibriumLoading,
            lastWaterTransferRateKgPerSecond: waterTransferRate,
            lastAdsorptionHeatW: adsorptionHeatW,
            operatingRegime: regime,
            hasReachedEquilibrium: nearEquilibrium);

        var waterStorageChangeKgPerSecond =
            (proposedState.AdsorbedWaterMassKg - _state.AdsorbedWaterMassKg) / dt;
        var storedEnergyChangeW =
            (proposedState.StoredThermalEnergyJ - _state.StoredThermalEnergyJ) / dt;

        // Adsorption heat is an internal conversion reflected in stream enthalpy and bed storage.
        var environmentalHeatLossW = _parameters.BedHeatLossCoefficientWPerK
            * (proposedBedTemperatureK - _parameters.AmbientTemperatureK);
        var energyInputW = inlet.DryAirMassFlowKgPerSecond * inlet.SpecificEnthalpyJPerKgDryAir
            + externalHeatW;
        var energyOutputW = outlet.DryAirMassFlowKgPerSecond * outlet.SpecificEnthalpyJPerKgDryAir
            + environmentalHeatLossW;

        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: inlet.DryAirMassFlowKgPerSecond,
            dryAirMassOutputKgPerSecond: outlet.DryAirMassFlowKgPerSecond,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: inlet.WaterVaporMassFlowKgPerSecond,
            waterMassOutputKgPerSecond: outlet.WaterVaporMassFlowKgPerSecond,
            waterMassStorageChangeKgPerSecond: waterStorageChangeKgPerSecond,
            energyInputW: energyInputW,
            energyOutputW: energyOutputW,
            storedEnergyChangeW: storedEnergyChangeW,
            timeStep: context.Simulation.TimeStep);

        LastWaterTransferRateKgPerSecond = waterTransferRate;
        LastAdsorptionHeatW = adsorptionHeatW;
        LastEquilibriumLoadingKgPerKg = equilibriumLoading;
        LastAvailableDesorptionHeatW = availableDesorptionHeatW;
        LastDesorptionWasEnergyLimited = limitedByEnergy;

        return new ComponentStepResult
        {
            OutputStates = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["outlet"] = outlet
            },
            ProposedInternalState = proposedState,
            Balance = balance,
            Diagnostics = diagnostics
        };
    }

    public void Commit(ComponentStepResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ProposedInternalState is SilicaGelState proposed)
        {
            _state = proposed;
        }

        _diagnostics.Clear();
        _diagnostics.AddRange(result.Diagnostics);
    }

    public IReadOnlyList<SimulationDiagnostic> GetDiagnostics() => _diagnostics;

    private double CalculateKineticCoefficient(double bedTemperatureK)
    {
        var k = _parameters.ReferenceMassTransferCoefficientPerSecond;
        if (_parameters.ActivationEnergyJPerMol <= 0.0)
        {
            return k;
        }

        var gasConstant = PhysicalConstants.UniversalGasConstantJPerMolK;
        return k * Math.Exp(
            -(_parameters.ActivationEnergyJPerMol / gasConstant)
            * ((1.0 / bedTemperatureK) - (1.0 / _parameters.ReferenceKineticTemperatureK)));
    }

    private double CalculatePressureDropPa(MoistAirState inlet)
    {
        var volumetricFlow = inlet.DryAirMassFlowKgPerSecond * inlet.SpecificVolumeM3PerKgDryAir;

        if (_parameters.EnableErgunPressureDrop)
        {
            var superficialVelocity = volumetricFlow / _parameters.BedCrossSectionAreaM2;
            var voidFraction = _parameters.BedVoidFraction;
            var particleDiameter = _parameters.ParticleDiameterM;
            var density = inlet.MoistAirDensityKgPerM3;
            var viscosity = ReferenceThermophysicalProperties.DryAirDynamicViscosityPaS;
            var oneMinus = 1.0 - voidFraction;
            var viscous = 150.0 * viscosity * oneMinus * oneMinus
                / (voidFraction * voidFraction * voidFraction * particleDiameter * particleDiameter)
                * superficialVelocity;
            var inertial = 1.75 * density * oneMinus
                / (voidFraction * voidFraction * voidFraction * particleDiameter)
                * superficialVelocity * superficialVelocity;
            return (viscous + inertial) * _parameters.BedLengthM;
        }

        if (_parameters.ReferencePressureDropPa <= 0.0)
        {
            return 0.0;
        }

        var ratio = volumetricFlow / _parameters.ReferenceVolumetricFlowM3PerSecond;
        return _parameters.ReferencePressureDropPa
            * Math.Pow(Math.Abs(ratio), _parameters.PressureDropFlowExponent);
    }

    /// <summary>
    /// Heat available to drive desorption: external heat, air-to-bed sensible gain,
    /// optional bed thermal drawdown, minus environmental loss
    /// (docs/03_Components/09_SilicaGel.md §65).
    /// </summary>
    private double EstimateAvailableDesorptionHeatW(
        MoistAirState inlet,
        double bedTemperatureK,
        double externalHeatW,
        double timeStepSeconds)
    {
        var capacityRate = inlet.DryAirMassFlowKgPerSecond
            * (ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK
               + inlet.HumidityRatioKgPerKgDryAir * ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK);

        var effectiveness = capacityRate <= 0.0 || _parameters.AirBedHeatTransferCoefficientWPerK <= 0.0
            ? 0.0
            : 1.0 - Math.Exp(-_parameters.AirBedHeatTransferCoefficientWPerK / capacityRate);

        var airToBedSensibleW = Math.Max(
            0.0,
            effectiveness * capacityRate * (inlet.TemperatureK - bedTemperatureK));

        var heatLossW = Math.Max(
            0.0,
            _parameters.BedHeatLossCoefficientWPerK * (bedTemperatureK - _parameters.AmbientTemperatureK));

        var bedCapacityJPerK = BedThermalCapacityJPerK(_state.WaterLoadingKgPerKgDryAdsorbent);
        var drawdownFloorK = Math.Max(
            _parameters.MinimumDesorptionBedTemperatureK,
            _parameters.AmbientTemperatureK);
        var thermalDrawdownW = timeStepSeconds > 0.0
            ? Math.Max(0.0, bedCapacityJPerK * (bedTemperatureK - drawdownFloorK) / timeStepSeconds)
            : 0.0;

        return Math.Max(0.0, Math.Max(0.0, externalHeatW) + airToBedSensibleW + thermalDrawdownW - heatLossW);
    }

    private double BedThermalCapacityJPerK(double loadingKgPerKg)
    {
        var adsorbedWaterKg = loadingKgPerKg * _parameters.DryAdsorbentMassKg;
        return _parameters.DryAdsorbentMassKg * _parameters.EffectiveSpecificHeatJPerKgK
            + adsorbedWaterKg * ReferenceThermophysicalProperties.LiquidWaterSpecificHeatJPerKgK
            + _parameters.BedHousingThermalCapacityJPerK;
    }

    private SilicaGelOperatingRegime ClassifyRegime(
        double drivingForce,
        double proposedLoading,
        bool nearEquilibrium)
    {
        if (nearEquilibrium)
        {
            if (proposedLoading >= _parameters.MaximumWaterLoadingKgPerKgDryAdsorbent
                - _parameters.NearEquilibriumLoadingToleranceKgPerKg)
            {
                return SilicaGelOperatingRegime.Saturated;
            }

            if (proposedLoading <= _parameters.MinimumRegeneratedLoadingKgPerKgDryAdsorbent
                + _parameters.NearEquilibriumLoadingToleranceKgPerKg)
            {
                return SilicaGelOperatingRegime.Regenerated;
            }

            return SilicaGelOperatingRegime.NearEquilibrium;
        }

        return drivingForce > 0.0
            ? SilicaGelOperatingRegime.Adsorption
            : SilicaGelOperatingRegime.Desorption;
    }

    private SimulationDiagnostic Diagnostic(
        ComponentStepContext context,
        string code,
        DiagnosticSeverity severity,
        string message)
        => new()
        {
            Code = code,
            Severity = severity,
            Message = message,
            ComponentId = Id,
            StepIndex = context.Simulation.StepIndex,
            SimulationTime = context.Simulation.ElapsedTime
        };

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
