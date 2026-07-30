namespace ThermoCore.AWG.Control;

/// <summary>Explicit fault codes used by the AWG controller.</summary>
public enum AwgFaultCode
{
    None,
    ConfigurationInvalid,
    CriticalSensorUnavailable,
    FanOperatingPointUnavailable,
    PeltierHotSideOverTemperature,
    BatteryBelowCriticalSoc,
    WaterTankFull,
    SimulationNonConvergent,
    EnergyBalanceInvalid,
    WaterBalanceInvalid,
    ComponentCriticalDiagnostic,
    SilicaGelOverTemperature,
    CollectorOverTemperature
}
