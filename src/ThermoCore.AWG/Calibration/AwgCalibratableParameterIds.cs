namespace ThermoCore.AWG.Calibration;

/// <summary>Stable ids for AWG parameters that may be fitted against measurements.</summary>
public static class AwgCalibratableParameterIds
{
    public const string CondenserBypassFactor = "condenser.bypassFactor";

    public const string CondenserDrainageEfficiency = "condenser.drainageEfficiency";

    public const string HeatRecoveryEffectiveness = "heatRecovery.effectivenessFraction";

    public const string SolarCollectorLossCoefficient = "solarCollector.overallLossCoefficientWPerM2K";

    public const string SilicaGelMassTransfer = "silicaGel.referenceMassTransferCoefficientPerSecond";
}
