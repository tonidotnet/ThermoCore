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

    /// <summary>Optional process-fan electrical power for plant-level COP (W).</summary>
    public double? FanElectricalPowerW { get; init; }
}
