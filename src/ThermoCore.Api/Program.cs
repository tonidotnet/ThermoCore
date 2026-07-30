using Microsoft.Extensions.Options;
using ThermoCore.Api.Endpoints;
using ThermoCore.Api.Services;
using ThermoCore.Core.Psychrometrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<ApiResourceLimits>(builder.Configuration.GetSection("ApiResourceLimits"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ApiResourceLimits>>().Value);
builder.Services.AddSingleton<IPsychrometricCalculator, PsychrometricCalculator>();
builder.Services.AddSingleton<PsychrometricApiService>();
builder.Services.AddSingleton<ConfigurationValidationService>();
builder.Services.AddSingleton<ISimulationJobStore, InMemorySimulationJobStore>();
builder.Services.AddSingleton<SimulationResultQueryService>();

var app = builder.Build();

app.MapOpenApi();

app.MapHealthEndpoints();
app.MapModelsEndpoints();
app.MapPsychrometricsEndpoints();
app.MapConfigurationEndpoints();
app.MapSimulationEndpoints();

app.Run();

