using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Components.Absorption;

/// <summary>Synthetic absorption research maps for feasibility screens (R7-001).</summary>
public static class AbsorptionPerformanceMapCatalog
{
    public const string GenericSolarThermalScreenProfileId = "absorption-generic-solar-thermal-screen";

    /// <summary>
    /// Coarse H2O/LiBr-style screen: generator 70/90°C, sink 30/40°C, evaporator 5/10°C.
    /// Thermal COP is intentionally modest (small-scale research caution in DOC-037).
    /// </summary>
    public static AbsorptionPerformanceMap CreateGenericSolarThermalScreen()
    {
        var points = new List<AbsorptionMapPoint>();
        foreach (var tgenC in new[] { 70.0, 90.0 })
        {
            foreach (var tsinkC in new[] { 30.0, 40.0 })
            {
                foreach (var tevapC in new[] { 5.0, 10.0 })
                {
                    var liftK = tsinkC - tevapC;
                    var driveK = tgenC - tsinkC;
                    var qgen = 400.0 + 2.0 * driveK;
                    var cop = Math.Clamp(0.55 - 0.008 * liftK + 0.004 * driveK, 0.15, 0.70);
                    var qc = qgen * cop;
                    points.Add(new AbsorptionMapPoint
                    {
                        GeneratorTemperatureK = UnitConversions.CelsiusToKelvin(tgenC),
                        HeatSinkTemperatureK = UnitConversions.CelsiusToKelvin(tsinkC),
                        EvaporatorTemperatureK = UnitConversions.CelsiusToKelvin(tevapC),
                        ThermalInputW = qgen,
                        CoolingOutputW = qc,
                        ThermalCop = cop
                    }.Validate());
                }
            }
        }

        return new AbsorptionPerformanceMap
        {
            ProfileId = GenericSolarThermalScreenProfileId,
            Manufacturer = "Generic",
            Model = "SolarThermalAbsorptionScreen-Demo",
            SourceIdentifier = "thermocore://samples/absorption/generic-solar-thermal-screen",
            SourceRevision = "2026-08-12",
            EvidenceLevel = TecEvidenceLevel.ProvisionalEngineering,
            Validity = AbsorptionValidityRange.FromPoints(points),
            MapPoints = points,
            ExtrapolationPolicy = AbsorptionExtrapolationPolicy.ClampWithDiagnostic,
            WorkingPair = "H2O/LiBr",
            FittingMethod = "Synthetic feasibility screen; IDW interpolation; research-only.",
            Notes = "Not for design. Replace with measured device data before any production path.",
            ResearchOnly = true
        }.Validate();
    }
}
