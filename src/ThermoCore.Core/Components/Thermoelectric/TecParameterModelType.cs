namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>Which physics/parameter representation a TEC profile supplies.</summary>
public enum TecParameterModelType
{
    /// <summary>Maps to <see cref="AnalyticalPeltierParameters"/> (α, R, K).</summary>
    AnalyticalSteadyState = 0,

    /// <summary>Constant cooling COP model (Fidelity Level 1).</summary>
    ConstantCop = 1,

    /// <summary>Reserved for commercial black-box / map models (R3).</summary>
    PerformanceMap = 2
}
