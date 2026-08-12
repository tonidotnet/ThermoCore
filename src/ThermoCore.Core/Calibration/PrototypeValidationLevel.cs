namespace ThermoCore.Core.Calibration;

/// <summary>How far a prototype campaign has been validated (VAL-003 / R3-001).</summary>
public enum PrototypeValidationLevel
{
    /// <summary>Lab bench / component-level tests only.</summary>
    BenchValidated = 0,

    /// <summary>Assembled system under controlled indoor conditions.</summary>
    IntegratedValidated = 1,

    /// <summary>Field / outdoor campaign with ambient weather.</summary>
    OutdoorValidated = 2
}
