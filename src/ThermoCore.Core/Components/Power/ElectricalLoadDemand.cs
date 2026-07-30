using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Graph;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Power;

/// <summary>
/// Prioritized electrical load demand (docs/03_Components/12_BatteryAndPowerManagement.md §7).
/// Lower <see cref="Priority"/> values are served first.
/// </summary>
public sealed record ElectricalLoadDemand
{
    public required string LoadId { get; init; }

    public required double RequestedPowerW { get; init; }

    public required int Priority { get; init; }

    public required bool IsEssential { get; init; }

    public double MinimumAcceptedPowerW { get; init; }

    public ElectricalLoadDemand Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(LoadId);
        FiniteNumber.RequireNonNegative(RequestedPowerW, nameof(RequestedPowerW));
        FiniteNumber.RequireNonNegative(MinimumAcceptedPowerW, nameof(MinimumAcceptedPowerW));
        if (MinimumAcceptedPowerW > RequestedPowerW)
        {
            throw new ArgumentException("Minimum accepted power cannot exceed requested power.");
        }

        return this;
    }
}
