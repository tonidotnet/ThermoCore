namespace ThermoCore.Core.Psychrometrics;

public sealed class PsychrometricInputException : PsychrometricException
{
    public PsychrometricInputException(string message)
        : base(message)
    {
    }

    public PsychrometricInputException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
