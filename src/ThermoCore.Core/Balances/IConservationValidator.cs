using ThermoCore.Core.Diagnostics;
using ThermoCore.Core.Numerics;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Balances;

public interface IConservationValidator
{
    BalanceValidationResult Validate(ConservationBalance balance, BalanceTolerance? tolerance = null);
}
