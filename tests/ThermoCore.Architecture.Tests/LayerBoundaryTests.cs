using System.Reflection;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Persistence;

namespace ThermoCore.Architecture.Tests;

/// <summary>DEV-008 layering rules via assembly reference inspection.</summary>
public class LayerBoundaryTests
{
    [Fact]
    public void Core_DoesNotReference_UpperLayers()
    {
        var forbidden = new[]
        {
            "ThermoCore.AWG",
            "ThermoCore.Application",
            "ThermoCore.Persistence",
            "ThermoCore.Api",
            "ThermoCore.Web",
            "ThermoCore.Console"
        };

        AssertNoReferences(typeof(PsychrometricCalculator).Assembly, forbidden);
    }

    [Fact]
    public void Awg_DoesNotReference_ApiWebApplication()
    {
        var forbidden = new[]
        {
            "ThermoCore.Application",
            "ThermoCore.Api",
            "ThermoCore.Web"
        };

        AssertNoReferences(typeof(AwgV3TopologyIds).Assembly, forbidden);
    }

    [Fact]
    public void Persistence_DoesNotReference_ApiOrWeb()
    {
        AssertNoReferences(
            typeof(IThermoCoreStore).Assembly,
            ["ThermoCore.Api", "ThermoCore.Web"]);
    }

    private static void AssertNoReferences(Assembly assembly, IEnumerable<string> forbiddenNames)
    {
        var referenced = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in forbiddenNames)
        {
            Assert.False(referenced.Contains(name), $"{assembly.GetName().Name} must not reference {name}.");
        }
    }
}
