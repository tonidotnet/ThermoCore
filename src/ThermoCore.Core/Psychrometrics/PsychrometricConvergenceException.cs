namespace ThermoCore.Core.Psychrometrics;

public sealed class PsychrometricConvergenceException : PsychrometricException
{
    public PsychrometricConvergenceException(string message)
        : base(message)
    {
    }

    public PsychrometricConvergenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
