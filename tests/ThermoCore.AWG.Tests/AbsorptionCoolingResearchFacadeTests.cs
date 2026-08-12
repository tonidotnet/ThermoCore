using ThermoCore.AWG.Cooling;
using ThermoCore.Core.Components.Absorption;
using ThermoCore.Core.Units;

namespace ThermoCore.AWG.Tests;

public class AbsorptionCoolingResearchFacadeTests
{
    [Fact]
    public void Facade_DelegatesToCoreResearchModel_AndStaysOutOfFactory()
    {
        var map = AbsorptionPerformanceMapCatalog.CreateGenericSolarThermalScreen();
        var facade = new AbsorptionCoolingResearchFacade(map);
        var result = facade.Evaluate(
            UnitConversions.CelsiusToKelvin(90.0),
            UnitConversions.CelsiusToKelvin(30.0),
            UnitConversions.CelsiusToKelvin(10.0));

        Assert.True(result.ResearchOnly);
        Assert.True(result.Feasible);
        Assert.Contains(result.Diagnostics, d => d.Code == "ABSORPTION.RESEARCH_ONLY");

        var cooling = new AwgCoolingPlantConfiguration
        {
            Technology = CoolingTechnology.AbsorptionResearch
        };
        Assert.ThrowsAny<ArgumentException>(() => cooling.Validate());
    }
}
