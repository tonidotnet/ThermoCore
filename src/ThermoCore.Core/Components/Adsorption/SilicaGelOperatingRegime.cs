using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

public enum SilicaGelOperatingRegime
{
    Idle,
    Adsorption,
    Desorption,
    NearEquilibrium,
    Saturated,
    Regenerated,
    Invalid
}
