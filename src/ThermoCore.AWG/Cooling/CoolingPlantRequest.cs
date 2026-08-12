using ThermoCore.Core.Graph;
using ThermoCore.Core.Psychrometrics;

namespace ThermoCore.AWG.Cooling;

/// <summary>Common cooling-plant evaluation request (ADR-016 / R4-001).</summary>
public sealed record CoolingPlantRequest
{
    public required MoistAirState Inlet { get; init; }

    public required SimulationContext Simulation { get; init; }

    /// <summary>Electrical power command (W). Used by commercial black-box and COP accounting.</summary>
    public double? ElectricalPowerW { get; init; }

    /// <summary>
    /// Available cold-side cooling capacity (W) for the thermoelectric condenser proxy.
    /// When null, falls back to <see cref="ElectricalPowerW"/> (COP≈1 proxy).
    /// </summary>
    public double? AvailableCoolingPowerW { get; init; }

    /// <summary>Cold surface / apparatus dew-point approach temperature (K).</summary>
    public double? ColdSurfaceTemperatureK { get; init; }

    /// <summary>Evaporating temperature for vapor-compression maps (K). Falls back to <see cref="ColdSurfaceTemperatureK"/>.</summary>
    public double? EvaporatingTemperatureK { get; init; }

    /// <summary>Condensing / rejection temperature for vapor-compression maps (K).</summary>
    public double? CondensingTemperatureK { get; init; }

    /// <summary>Normalized compressor speed for vapor-compression maps in [0, 1].</summary>
    public double? CompressorSpeedFraction { get; init; }

    /// <summary>Explicit compressor enable request; null means inferred from speed/power.</summary>
    public bool? CompressorRequested { get; init; }

    /// <summary>Optional discharge-gas temperature for VC safety diagnostics (K).</summary>
    public double? DischargeTemperatureK { get; init; }

    /// <summary>Optional process-fan electrical power for plant-level COP (W).</summary>
    public double? FanElectricalPowerW { get; init; }
}
