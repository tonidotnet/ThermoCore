using ThermoCore.Core.Components.Thermoelectric;
using ThermoCore.Core.Units;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>Reference vapor-compression maps for tests and demos (R5-001).</summary>
public static class VaporCompressionPerformanceMapCatalog
{
    public const string GenericSmallDcModuleProfileId = "vc-generic-small-dc-module";

    /// <summary>
    /// Synthetic manufacturer-style grid for a small DC refrigeration module.
    /// Points are exact; interpolation between them is deterministic IDW.
    /// </summary>
    public static VaporCompressionPerformanceMap CreateGenericSmallDcModuleReference()
    {
        // Grid: Tevap ∈ {5°C, 10°C}, Tcond ∈ {35°C, 45°C}, speed ∈ {0.5, 1.0}
        // Qc decreases with lift; Pe rises mildly with lift and speed.
        var points = new List<VaporCompressionMapPoint>();
        foreach (var tevapC in new[] { 5.0, 10.0 })
        {
            foreach (var tcondC in new[] { 35.0, 45.0 })
            {
                foreach (var speed in new[] { 0.5, 1.0 })
                {
                    var liftK = tcondC - tevapC;
                    var qc = speed * (220.0 - 2.5 * liftK);
                    var pe = speed * (70.0 + 0.8 * liftK);
                    points.Add(new VaporCompressionMapPoint
                    {
                        EvaporatingTemperatureK = UnitConversions.CelsiusToKelvin(tevapC),
                        CondensingTemperatureK = UnitConversions.CelsiusToKelvin(tcondC),
                        SpeedFraction = speed,
                        CoolingCapacityW = qc,
                        ElectricalPowerW = pe,
                        Cop = qc / pe
                    }.Validate());
                }
            }
        }

        return new VaporCompressionPerformanceMap
        {
            ProfileId = GenericSmallDcModuleProfileId,
            Manufacturer = "Generic",
            Model = "SmallDcRefrigerationModule-Demo",
            SourceIdentifier = "thermocore://samples/vapor-compression/generic-small-dc-module",
            SourceRevision = "2026-08-12",
            EvidenceLevel = TecEvidenceLevel.ProvisionalEngineering,
            Validity = VaporCompressionValidityRange.FromPoints(points),
            MapPoints = points,
            ExtrapolationPolicy = VaporCompressionExtrapolationPolicy.ClampWithDiagnostic,
            Cycling = new VaporCompressionCyclingLimits
            {
                MinimumRuntime = TimeSpan.FromMinutes(3),
                MinimumOffTime = TimeSpan.FromMinutes(3)
            },
            Safety = new VaporCompressionSafetyLimits
            {
                FrostThresholdEvaporatingTemperatureK = UnitConversions.CelsiusToKelvin(0.0),
                MaximumCondensingTemperatureK = UnitConversions.CelsiusToKelvin(55.0),
                MaximumDischargeTemperatureK = UnitConversions.CelsiusToKelvin(95.0)
            },
            EvaporatorUaWPerK = 35.0,
            CondenserUaWPerK = 45.0,
            FanElectricalPowerW = 8.0,
            Refrigerant = "R134a",
            FittingMethod = "Synthetic manufacturer grid; inverse-distance interpolation; clamp-out-of-range.",
            Notes = "Stand-in map for R5-001 contract tests. Replace with measured module datasheet points."
        }.Validate();
    }
}
