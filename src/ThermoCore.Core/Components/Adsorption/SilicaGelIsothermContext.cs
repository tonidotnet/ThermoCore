using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Adsorption;

public sealed record SilicaGelIsothermContext
{
    public bool PreferDesorptionBranch { get; init; }
}
