namespace ThermoCore.AWG.Tests;

public class SmokeTests
{
    [Fact]
    public void AwgModule_Name_IsStable()
    {
        Assert.Equal("ThermoCore.AWG", ThermoCore.AWG.AwgModule.Name);
    }
}
