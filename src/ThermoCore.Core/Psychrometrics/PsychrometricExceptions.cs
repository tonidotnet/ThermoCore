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
