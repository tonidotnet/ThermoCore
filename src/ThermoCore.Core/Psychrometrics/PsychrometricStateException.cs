namespace ThermoCore.Core.Psychrometrics;

public sealed class PsychrometricStateException : PsychrometricException
{
    public PsychrometricStateException(string message)
        : base(message)
    {
    }

    public PsychrometricStateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
