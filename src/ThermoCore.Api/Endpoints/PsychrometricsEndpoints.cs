using ThermoCore.Api.Contracts;
using ThermoCore.Api.Services;

namespace ThermoCore.Api.Endpoints;

public static class PsychrometricsEndpoints
{
    public static IEndpointRouteBuilder MapPsychrometricsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/psychrometrics/calculate", (
                PsychrometricCalculateRequest request,
                PsychrometricApiService service) =>
            {
                try
                {
                    return Results.Ok(service.Calculate(request));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new
                    {
                        title = "Validation error",
                        status = 400,
                        detail = ex.Message,
                        errors = new[]
                        {
                            new ValidationIssueDto
                            {
                                Path = ex.ParamName ?? "request",
                                Code = "ValueOutOfRange",
                                Message = ex.Message
                            }
                        }
                    });
                }
            })
            .WithName("CalculatePsychrometrics")
            .WithTags("Psychrometrics");

        return app;
    }
}
