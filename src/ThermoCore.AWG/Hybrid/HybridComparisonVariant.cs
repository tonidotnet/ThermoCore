namespace ThermoCore.AWG.Hybrid;

/// <summary>Hybrid architecture variants from DOC-038 (R6-001 / HYB-001…002).</summary>
public enum HybridComparisonVariant
{
    /// <summary>A — ambient → TEC cooling → water.</summary>
    DirectTec = 0,

    /// <summary>B — ambient → solar heating → TEC cooling (scientific control).</summary>
    HeatingOnlyControl = 1,

    /// <summary>C — regeneration stream → TEC condensation.</summary>
    SorbentPlusTec = 2,

    /// <summary>D — ambient → vapor-compression cooling → water.</summary>
    DirectCompressor = 3,

    /// <summary>E — regeneration stream → vapor-compression condensation.</summary>
    SorbentPlusCompressor = 4
}
