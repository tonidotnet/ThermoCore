namespace ThermoCore.Core.Calibration;

/// <summary>Outcome of a bounded parameter fitting run.</summary>
public sealed record ParameterFittingResult
{
    public required IReadOnlyDictionary<string, double> InitialValues { get; init; }

    public required IReadOnlyDictionary<string, double> FittedValues { get; init; }

    public required double InitialObjective { get; init; }

    public required double FinalObjective { get; init; }

    public required int EvaluationCount { get; init; }

    public required int PassCount { get; init; }

    public bool Improved => FinalObjective < InitialObjective;
}
