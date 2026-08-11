namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>Built-in reference TEC profiles (no OEM hard-coding in the physics model).</summary>
public static class TecManufacturerProfileCatalog
{
    public const string GenericTec112706ProfileId = "generic-tec1-12706";

    /// <summary>
    /// Generic TEC1-12706-class reference profile for tests and early sizing.
    /// Explicit analytical coefficients match
    /// <see cref="AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults"/>;
    /// datasheet ratings are conventional 40×40 mm class values with provisional evidence.
    /// </summary>
    public static TecManufacturerProfile CreateGenericTec112706Reference()
    {
        var defaults = AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults();
        return new TecManufacturerProfile
        {
            SchemaVersion = TecManufacturerProfile.CurrentSchemaVersion,
            ProfileId = GenericTec112706ProfileId,
            Manufacturer = "Generic",
            Model = "TEC1-12706",
            ParameterModelType = TecParameterModelType.AnalyticalSteadyState,
            SourceIdentifier = "thermocore://profiles/generic-tec1-12706",
            SourceRevision = "2026-08-12",
            EvidenceLevel = TecEvidenceLevel.ProvisionalEngineering,
            LengthMm = 40.0,
            WidthMm = 40.0,
            HeightMm = 3.6,
            MaximumCurrentA = defaults.MaximumCurrentA,
            MaximumVoltageV = defaults.MaximumVoltageV,
            MaximumCoolingPowerW = 50.0,
            MaximumTemperatureDifferenceK = defaults.MaximumTemperatureDifferenceK!.Value,
            HotSideReferenceTemperatureK = 300.0,
            MinimumColdSideTemperatureK = defaults.MinimumColdSideTemperatureK,
            MaximumHotSideTemperatureK = defaults.MaximumHotSideTemperatureK,
            MaximumElectricalPowerW = defaults.MaximumElectricalPowerW,
            AnalyticalCoefficients = new TecAnalyticalCoefficientSet
            {
                SeebeckCoefficientVPerK = defaults.SeebeckCoefficientVPerK,
                ElectricalResistanceOhm = defaults.ElectricalResistanceOhm,
                ThermalConductanceWPerK = defaults.ThermalConductanceWPerK
            },
            FittingMethod =
                "Explicit provisional α,R,K identical to AnalyticalPeltierParameters.CreateProvisionalEngineeringDefaults(); " +
                "not a manufacturer commitment.",
            Notes =
                "Generic 40×40 mm class reference for ThermoCore tests. Replace with a datasheet-backed profile before predictive design."
        }.Validate();
    }
}
