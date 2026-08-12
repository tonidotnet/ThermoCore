namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>Behavior when a map query falls outside the measured validity box (COOL-006).</summary>
public enum VaporCompressionExtrapolationPolicy
{
    /// <summary>Clamp axes to validity and emit <c>VC.OUTSIDE_VALIDITY</c>; no true extrapolation.</summary>
    ClampWithDiagnostic = 0,

    /// <summary>Refuse the operating point; capacity/power set to zero with <c>VC.EXTRAPOLATION_REJECTED</c>.</summary>
    Reject = 1
}
