using ThermoCore.Core.Physics;

namespace ThermoCore.Core.Tests;

public class PhysicalConstantsTests
{
    [Fact]
    public void CelsiusOffset_IsExactDefinition()
    {
        Assert.Equal(273.15, PhysicalConstants.CelsiusOffsetK);
    }

    [Fact]
    public void MolecularMassRatio_IsPositiveFinite()
    {
        Assert.True(double.IsFinite(PhysicalConstants.MolecularMassRatio));
        Assert.True(PhysicalConstants.MolecularMassRatio > 0.0);
    }

    [Fact]
    public void GasConstants_ArePositive()
    {
        Assert.True(PhysicalConstants.DryAirGasConstantJPerKgK > 0.0);
        Assert.True(PhysicalConstants.WaterVaporGasConstantJPerKgK > 0.0);
        Assert.True(PhysicalConstants.UniversalGasConstantJPerMolK > 0.0);
    }

    [Fact]
    public void ReferenceHeatCapacities_ArePositive()
    {
        Assert.True(ReferenceThermophysicalProperties.DryAirSpecificHeatJPerKgK > 0.0);
        Assert.True(ReferenceThermophysicalProperties.WaterVaporSpecificHeatJPerKgK > 0.0);
        Assert.True(ReferenceThermophysicalProperties.LiquidWaterSpecificHeatJPerKgK > 0.0);
        Assert.True(ReferenceThermophysicalProperties.ReferenceVaporizationEnthalpyJPerKg > 0.0);
    }
}
