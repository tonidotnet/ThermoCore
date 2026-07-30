using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Numerics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Balances;

public sealed record BalanceValidationResult
{
    public required bool IsValid { get; init; }

    public required double RelativeDryAirMassResidual { get; init; }

    public required double RelativeWaterMassResidual { get; init; }

    public required double RelativeEnergyResidual { get; init; }

    public required double RelativeElectricalEnergyResidual { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}
