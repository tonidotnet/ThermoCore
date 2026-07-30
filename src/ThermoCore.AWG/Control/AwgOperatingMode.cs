namespace ThermoCore.AWG.Control;

/// <summary>Supervisory operating modes for ThermoCore.AWG (docs/04_Simulation/14_ControlSystem.md §4).</summary>
public enum AwgOperatingMode
{
    Off,
    Startup,
    Adsorption,
    Regeneration,
    Condensation,
    HeatRecovery,
    Recirculation,
    Standby,
    ControlledShutdown,
    Fault
}
