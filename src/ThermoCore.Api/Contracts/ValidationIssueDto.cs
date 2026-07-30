namespace ThermoCore.Api.Contracts;

public sealed record ValidationIssueDto
{
    public required string Path { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? ExpectedRange { get; init; }
}
