using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

public enum AwgFanControlStrategy
{
    FixedControlFraction,
    FixedDryAirMassFlow,
    FixedVolumetricFlow,
    PressureControlled,
    OptimizationControlled
}
