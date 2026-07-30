namespace ThermoCore.Api.Contracts;

public sealed record ConfigurationValidateResponse
{
    public required bool IsValid { get; init; }

    public required IReadOnlyList<ValidationIssueDto> Errors { get; init; }

    public required IReadOnlyList<ValidationIssueDto> Warnings { get; init; }
}
