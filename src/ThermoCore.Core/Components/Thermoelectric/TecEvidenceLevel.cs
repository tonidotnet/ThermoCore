namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>Provenance / confidence of a TEC hardware profile (COOL-003).</summary>
public enum TecEvidenceLevel
{
    /// <summary>Engineering placeholder; not for predictive design.</summary>
    ProvisionalEngineering = 0,

    /// <summary>Values transcribed from a manufacturer datasheet.</summary>
    ManufacturerDatasheet = 1,

    /// <summary>Fitted to laboratory or prototype measurements.</summary>
    MeasuredPrototype = 2,

    /// <summary>Cross-checked datasheet + measurement calibration.</summary>
    Calibrated = 3
}
