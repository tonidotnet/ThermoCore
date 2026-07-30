using System.Text.Json;
using ThermoCore.Api.Services;

namespace ThermoCore.Api.Endpoints;

public static class ConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/configurations/validate", async (
                HttpRequest httpRequest,
                ConfigurationValidationService service) =>
            {
                using var document = await JsonDocument.ParseAsync(httpRequest.Body);
                var response = service.Validate(document.RootElement);
                return response.IsValid
                    ? Results.Ok(response)
                    : Results.BadRequest(response);
            })
            .WithName("ValidateConfiguration")
            .WithTags("Configurations");

        return app;
    }
}
