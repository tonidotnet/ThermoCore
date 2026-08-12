namespace ThermoCore.AWG.Cooling;

/// <summary>AWG cooling-plant technology selection (COOL-002 / R4-001).</summary>
public enum CoolingTechnology
{
    /// <summary>Existing ControllableHeatSource + Condenser Peltier proxy path.</summary>
    Thermoelectric = 0,

    /// <summary>Empirical commercial Peltier dehumidifier black-box (Core R3-002).</summary>
    CommercialPeltierDehumidifier = 1,

    /// <summary>Vapor-compression map-based plant (R5-001 map + R5-002 adapter).</summary>
    VaporCompression = 2,

    /// <summary>Reserved for R7 absorption research models.</summary>
    AbsorptionResearch = 3
}
