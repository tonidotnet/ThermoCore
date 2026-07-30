using System.Text.Json;
using ThermoCore.Api.Contracts;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Topology;
using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Api.Services;

/// <summary>Validates AWG configuration documents for the API (API-004).</summary>
public sealed class ConfigurationValidationService
{
    private readonly AwgV3SystemGraphBuilder _graphBuilder = new();

    public ConfigurationValidateResponse Validate(JsonElement configurationJson)
    {
        var errors = new List<ValidationIssueDto>();
        var warnings = new List<ValidationIssueDto>();

        try
        {
            var json = configurationJson.GetRawText();
            var document = AwgConfigurationLoader.LoadFromJson(json);
            _ = _graphBuilder.Build(document.System, document.InitialState);
        }
        catch (AwgConfigurationException ex)
        {
            foreach (var diagnostic in ex.Diagnostics)
            {
                var issue = ToIssue(diagnostic);
                if (diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Critical)
                {
                    errors.Add(issue);
                }
                else
                {
                    warnings.Add(issue);
                }
            }

            if (errors.Count == 0)
            {
                errors.Add(new ValidationIssueDto
                {
                    Path = "configuration",
                    Code = "AwgConfigurationException",
                    Message = ex.Message
                });
            }
        }
        catch (JsonException ex)
        {
            errors.Add(new ValidationIssueDto
            {
                Path = "configuration",
                Code = "InvalidJson",
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            errors.Add(new ValidationIssueDto
            {
                Path = ex.ParamName ?? "configuration",
                Code = "ValueOutOfRange",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationIssueDto
            {
                Path = "configuration",
                Code = "ValidationFailed",
                Message = ex.Message
            });
        }

        return new ConfigurationValidateResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private static ValidationIssueDto ToIssue(SimulationDiagnostic diagnostic)
        => new()
        {
            Path = diagnostic.ComponentId ?? "configuration",
            Code = diagnostic.Code,
            Message = diagnostic.Message
        };
}
