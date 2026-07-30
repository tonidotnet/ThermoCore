namespace ThermoCore.Api.Services;

/// <summary>Outcome of looking up a completed simulation job for result queries.</summary>
public sealed class CompletedJobLookupResult
{
    private CompletedJobLookupResult(SimulationJob? job, int? statusCode, string? detail)
    {
        Job = job;
        StatusCode = statusCode;
        Detail = detail;
    }

    public SimulationJob? Job { get; }

    public int? StatusCode { get; }

    public string? Detail { get; }

    public bool Succeeded => Job is not null;

    public static CompletedJobLookupResult Ok(SimulationJob job)
        => new(job, null, null);

    public static CompletedJobLookupResult NotFound()
        => new(null, 404, null);

    public static CompletedJobLookupResult Conflict(string detail)
        => new(null, 409, detail);
}
