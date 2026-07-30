using ThermoCore.Core.Diagnostics;

namespace ThermoCore.AWG.Control;

public interface IAwgController
{
    AwgControlStepResult Evaluate(
        AwgSystemObservation observation,
        AwgControllerState currentState,
        AwgControlParameters parameters,
        TimeSpan timeStep);
}
