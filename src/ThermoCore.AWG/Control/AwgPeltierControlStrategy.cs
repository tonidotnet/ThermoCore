using ThermoCore.Core.Validation;

namespace ThermoCore.AWG.Control;

public enum AwgPeltierControlStrategy
{
    FixedPower,
    MaximumAvailablePower,
    TargetColdSideTemperature,
    TargetDewPointApproach,
    MinimumWhPerLiter,
    ThermalProtectionLimited
}
