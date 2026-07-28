using ThermoCore.Core.Physics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Psychrometrics;

/// <summary>
/// Deterministic psychrometric calculator (docs/02_Mathematics/05_Psychrometrics.md).
/// </summary>
public sealed class PsychrometricCalculator : IPsychrometricCalculator
{
    private readonly ISaturationPressureProvider _saturationPressureProvider;
    private readonly PsychrometricTolerances _tolerances;

    public PsychrometricCalculator(
        ISaturationPressureProvider? saturationPressureProvider = null,
        PsychrometricTolerances? tolerances = null)
    {
        _saturationPressureProvider = saturationPressureProvider ?? BuckSaturationPressureProvider.Instance;
        _tolerances = tolerances ?? PsychrometricTolerances.Default;
    }

    public MoistAirState CreateFromRelativeHumidity(
        double temperatureK,
        double pressurePa,
        double relativeHumidityFraction,
        double dryAirMassFlowKgPerSecond)
    {
        ValidateTemperature(temperatureK);
        ValidatePressure(pressurePa);
        ValidateRelativeHumidityInput(relativeHumidityFraction);
        ValidateDryAirMassFlow(dryAirMassFlowKgPerSecond);

        var vaporPressurePa = CalculateVaporPressureFromRelativeHumidityPa(temperatureK, relativeHumidityFraction);
        var humidityRatio = CalculateHumidityRatio(pressurePa, vaporPressurePa);
        return CreateCommittedState(
            temperatureK,
            pressurePa,
            humidityRatio,
            dryAirMassFlowKgPerSecond,
            vaporPressurePa);
    }

    public MoistAirState CreateFromHumidityRatio(
        double temperatureK,
        double pressurePa,
        double humidityRatioKgPerKgDryAir,
        double dryAirMassFlowKgPerSecond)
    {
        ValidateTemperature(temperatureK);
        ValidatePressure(pressurePa);
        ValidateHumidityRatio(humidityRatioKgPerKgDryAir);
        ValidateDryAirMassFlow(dryAirMassFlowKgPerSecond);

        var vaporPressurePa = CalculateVaporPressurePa(pressurePa, humidityRatioKgPerKgDryAir);
        return CreateCommittedState(
            temperatureK,
            pressurePa,
            humidityRatioKgPerKgDryAir,
            dryAirMassFlowKgPerSecond,
            vaporPressurePa);
    }

    public MoistAirState CreateFromDewPoint(
        double temperatureK,
        double pressurePa,
        double dewPointTemperatureK,
        double dryAirMassFlowKgPerSecond)
    {
        ValidateTemperature(temperatureK);
        ValidatePressure(pressurePa);
        ValidateTemperature(dewPointTemperatureK);
        ValidateDryAirMassFlow(dryAirMassFlowKgPerSecond);

        var vaporPressurePa = CalculateSaturationPressurePa(dewPointTemperatureK);
        var humidityRatio = CalculateHumidityRatio(pressurePa, vaporPressurePa);
        return CreateCommittedState(
            temperatureK,
            pressurePa,
            humidityRatio,
            dryAirMassFlowKgPerSecond,
            vaporPressurePa);
    }

    public double CalculateSaturationPressurePa(double temperatureK)
        => _saturationPressureProvider.CalculatePressurePa(temperatureK);

    public double CalculateHumidityRatio(double pressurePa, double vaporPressurePa)
    {
        FiniteNumber.Require(pressurePa, nameof(pressurePa));
        FiniteNumber.Require(vaporPressurePa, nameof(vaporPressurePa));

        if (vaporPressurePa < 0.0 || vaporPressurePa >= pressurePa)
        {
            throw new PsychrometricStateException(
                "Vapor pressure must be non-negative and lower than total pressure.");
        }

        return PhysicalConstants.MolecularMassRatio * vaporPressurePa / (pressurePa - vaporPressurePa);
    }

    public double CalculateVaporPressurePa(double pressurePa, double humidityRatioKgPerKgDryAir)
    {
        FiniteNumber.RequirePositive(pressurePa, nameof(pressurePa));
        ValidateHumidityRatio(humidityRatioKgPerKgDryAir);

        var vaporPressurePa = humidityRatioKgPerKgDryAir * pressurePa
            / (PhysicalConstants.MolecularMassRatio + humidityRatioKgPerKgDryAir);

        if (vaporPressurePa < 0.0 || vaporPressurePa >= pressurePa)
        {
            throw new PsychrometricStateException(
                "Derived vapor pressure must be non-negative and lower than total pressure.");
        }

        return vaporPressurePa;
    }

    public double CalculateVaporPressureFromRelativeHumidityPa(
        double temperatureK,
        double relativeHumidityFraction)
    {
        ValidateRelativeHumidityInput(relativeHumidityFraction);
        return relativeHumidityFraction * CalculateSaturationPressurePa(temperatureK);
    }

    public double? CalculateDewPointTemperatureK(double vaporPressurePa)
    {
        FiniteNumber.Require(vaporPressurePa, nameof(vaporPressurePa));

        if (vaporPressurePa <= _tolerances.PressurePa)
        {
            return null;
        }

        var low = _saturationPressureProvider.ModelInfo.MinimumTemperatureK;
        var high = _saturationPressureProvider.ModelInfo.MaximumTemperatureK;
        var pressureLow = CalculateSaturationPressurePa(low);
        var pressureHigh = CalculateSaturationPressurePa(high);

        if (vaporPressurePa < pressureLow || vaporPressurePa > pressureHigh)
        {
            throw new PsychrometricInputException(
                $"Vapor pressure {vaporPressurePa} Pa is outside the saturation model pressure range " +
                $"[{pressureLow}, {pressureHigh}] Pa.");
        }

        for (var iteration = 0; iteration < _tolerances.MaximumRootIterations; iteration++)
        {
            var mid = 0.5 * (low + high);
            var pressureMid = CalculateSaturationPressurePa(mid);
            var pressureResidual = Math.Abs(pressureMid - vaporPressurePa);

            if (pressureResidual <= _tolerances.PressurePa)
            {
                return mid;
            }

            if (high - low <= _tolerances.TemperatureK && pressureResidual <= 10.0 * _tolerances.PressurePa)
            {
                return mid;
            }

            if (pressureMid < vaporPressurePa)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        throw new PsychrometricConvergenceException(
            $"Dew-point inversion did not converge within {_tolerances.MaximumRootIterations} iterations.");
    }

    public double CalculateSpecificEnthalpyJPerKgDryAir(double temperatureK, double humidityRatioKgPerKgDryAir)
    {
        FiniteNumber.Require(temperatureK, nameof(temperatureK));
        ValidateHumidityRatio(humidityRatioKgPerKgDryAir);

        var temperatureC = temperatureK - PhysicalConstants.CelsiusOffsetK;
        return ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK * temperatureC
            + humidityRatioKgPerKgDryAir
            * (ReferenceThermophysicalProperties.ReferenceVaporizationEnthalpyJPerKg
               + ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK * temperatureC);
    }

    public double CalculateTemperatureKFromEnthalpy(
        double specificEnthalpyJPerKgDryAir,
        double humidityRatioKgPerKgDryAir)
    {
        FiniteNumber.Require(specificEnthalpyJPerKgDryAir, nameof(specificEnthalpyJPerKgDryAir));
        ValidateHumidityRatio(humidityRatioKgPerKgDryAir);

        var denominator = ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK
            + humidityRatioKgPerKgDryAir * ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK;

        FiniteNumber.RequirePositive(denominator, nameof(denominator));

        var temperatureC = (specificEnthalpyJPerKgDryAir
            - humidityRatioKgPerKgDryAir * ReferenceThermophysicalProperties.ReferenceVaporizationEnthalpyJPerKg)
            / denominator;

        return temperatureC + PhysicalConstants.CelsiusOffsetK;
    }

    public double CalculateSpecificVolumeM3PerKgDryAir(
        double temperatureK,
        double pressurePa,
        double humidityRatioKgPerKgDryAir)
    {
        FiniteNumber.RequirePositive(temperatureK, nameof(temperatureK));
        FiniteNumber.RequirePositive(pressurePa, nameof(pressurePa));
        ValidateHumidityRatio(humidityRatioKgPerKgDryAir);

        return PhysicalConstants.DryAirGasConstantJPerKgK * temperatureK / pressurePa
            * (1.0 + humidityRatioKgPerKgDryAir / PhysicalConstants.MolecularMassRatio);
    }

    public double CalculateMoistAirDensityKgPerM3(
        double temperatureK,
        double pressurePa,
        double humidityRatioKgPerKgDryAir)
    {
        var specificVolume = CalculateSpecificVolumeM3PerKgDryAir(
            temperatureK,
            pressurePa,
            humidityRatioKgPerKgDryAir);

        FiniteNumber.RequirePositive(specificVolume, nameof(specificVolume));
        return (1.0 + humidityRatioKgPerKgDryAir) / specificVolume;
    }

    private MoistAirState CreateCommittedState(
        double temperatureK,
        double pressurePa,
        double humidityRatioKgPerKgDryAir,
        double dryAirMassFlowKgPerSecond,
        double vaporPressurePa)
    {
        var saturationPressurePa = CalculateSaturationPressurePa(temperatureK);
        if (saturationPressurePa >= pressurePa)
        {
            throw new PsychrometricStateException(
                "Saturation pressure must be lower than total pressure for the initial moist-air model.");
        }

        var relativeHumidityFraction = vaporPressurePa / saturationPressurePa;
        FiniteNumber.Require(relativeHumidityFraction, nameof(relativeHumidityFraction));

        var phaseState = ClassifyPhase(relativeHumidityFraction);
        if (phaseState == MoistAirPhaseState.Saturated
            && relativeHumidityFraction > 1.0
            && relativeHumidityFraction <= 1.0 + _tolerances.RelativeHumidityFraction)
        {
            relativeHumidityFraction = 1.0;
        }

        var dewPointTemperatureK = CalculateDewPointTemperatureK(vaporPressurePa)
            ?? _saturationPressureProvider.ModelInfo.MinimumTemperatureK;

        var enthalpy = CalculateSpecificEnthalpyJPerKgDryAir(temperatureK, humidityRatioKgPerKgDryAir);
        var specificVolume = CalculateSpecificVolumeM3PerKgDryAir(
            temperatureK,
            pressurePa,
            humidityRatioKgPerKgDryAir);
        var density = (1.0 + humidityRatioKgPerKgDryAir) / specificVolume;
        var vaporMassFlow = humidityRatioKgPerKgDryAir * dryAirMassFlowKgPerSecond;

        return new MoistAirState
        {
            TemperatureK = temperatureK,
            PressurePa = pressurePa,
            HumidityRatioKgPerKgDryAir = humidityRatioKgPerKgDryAir,
            DryAirMassFlowKgPerSecond = dryAirMassFlowKgPerSecond,
            VaporPressurePa = vaporPressurePa,
            RelativeHumidityFraction = relativeHumidityFraction,
            DewPointTemperatureK = dewPointTemperatureK,
            SpecificEnthalpyJPerKgDryAir = enthalpy,
            SpecificVolumeM3PerKgDryAir = specificVolume,
            MoistAirDensityKgPerM3 = density,
            WaterVaporMassFlowKgPerSecond = vaporMassFlow,
            PhaseState = phaseState
        };
    }

    private MoistAirPhaseState ClassifyPhase(double relativeHumidityFraction)
    {
        if (relativeHumidityFraction > 1.0 + _tolerances.RelativeHumidityFraction)
        {
            return MoistAirPhaseState.SupersaturatedCandidate;
        }

        if (relativeHumidityFraction >= 1.0 - _tolerances.RelativeHumidityFraction)
        {
            return MoistAirPhaseState.Saturated;
        }

        return MoistAirPhaseState.Unsaturated;
    }

    private static void ValidateTemperature(double temperatureK)
    {
        FiniteNumber.Require(temperatureK, nameof(temperatureK));
        if (temperatureK < 228.15 || temperatureK > 373.15)
        {
            throw new PsychrometricInputException(
                $"Temperature {temperatureK} K is outside the supported range [228.15, 373.15] K.");
        }
    }

    private static void ValidatePressure(double pressurePa)
    {
        FiniteNumber.RequirePositive(pressurePa, nameof(pressurePa));
        if (pressurePa < 50_000.0 || pressurePa > 120_000.0)
        {
            throw new PsychrometricInputException(
                $"Pressure {pressurePa} Pa is outside the supported range [50000, 120000] Pa.");
        }
    }

    private static void ValidateRelativeHumidityInput(double relativeHumidityFraction)
    {
        FiniteNumber.Require(relativeHumidityFraction, nameof(relativeHumidityFraction));
        if (relativeHumidityFraction < 0.0 || relativeHumidityFraction > 1.0)
        {
            throw new PsychrometricInputException(
                "Relative humidity fraction must be in [0, 1]. Values above 1 must not be clamped.");
        }
    }

    private static void ValidateHumidityRatio(double humidityRatioKgPerKgDryAir)
    {
        FiniteNumber.RequireNonNegative(humidityRatioKgPerKgDryAir, nameof(humidityRatioKgPerKgDryAir));
        if (humidityRatioKgPerKgDryAir > 1.0)
        {
            throw new PsychrometricInputException(
                "Humidity ratio must be in [0, 1] kg/kg dry air for the initial model.");
        }
    }

    private static void ValidateDryAirMassFlow(double dryAirMassFlowKgPerSecond)
    {
        FiniteNumber.RequireNonNegative(dryAirMassFlowKgPerSecond, nameof(dryAirMassFlowKgPerSecond));
    }
}
