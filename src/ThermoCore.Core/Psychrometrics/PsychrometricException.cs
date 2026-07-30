namespace ThermoCore.Core.Psychrometrics;

public class PsychrometricException : Exception
{
    public PsychrometricException(string message)
        : base(message)
    {
    }

    public PsychrometricException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
