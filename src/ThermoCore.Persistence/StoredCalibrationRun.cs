namespace ThermoCore.Persistence;

/// <summary>Persisted calibration provenance (DOC-021 §14 / CAL-007).</summary>
public sealed record StoredCalibrationRun
{
    public required Guid Id { get; init; }

    public required string MeasurementSourcePath { get; init; }

    public required Guid? BaselineConfigurationVersionId { get; init; }

    public required Guid? FittedConfigurationVersionId { get; init; }

    public required string Algorithm { get; init; }

    public required string ParameterIdsJson { get; init; }

    public required string InitialValuesJson { get; init; }

    public required string FittedValuesJson { get; init; }

    public required double InitialObjective { get; init; }

    public required double FinalObjective { get; init; }

    public required int EvaluationCount { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
