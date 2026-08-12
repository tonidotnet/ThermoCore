using ThermoCore.Core.Components.Absorption;

namespace ThermoCore.AWG.Cooling;

/// <summary>
/// AWG-facing research facade for absorption feasibility (R7-001 / COOL-008).
/// Does not implement <see cref="ICoolingPlantModel"/> — absorption stays out of production selection.
/// </summary>
public sealed class AbsorptionCoolingResearchFacade
{
    private readonly AbsorptionCoolingResearchModel _model;

    public AbsorptionCoolingResearchFacade(AbsorptionPerformanceMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        _model = new AbsorptionCoolingResearchModel(map);
    }

    public AbsorptionPerformanceMap Map => _model.Map;

    public AbsorptionFeasibilityResult Evaluate(
        double generatorTemperatureK,
        double heatSinkTemperatureK,
        double evaporatorTemperatureK)
        => _model.Evaluate(generatorTemperatureK, heatSinkTemperatureK, evaporatorTemperatureK);
}
