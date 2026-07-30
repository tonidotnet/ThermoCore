using ThermoCore.Api.Endpoints;
using ThermoCore.Api.Services;
using ThermoCore.Core.Psychrometrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IPsychrometricCalculator, PsychrometricCalculator>();
builder.Services.AddSingleton<PsychrometricApiService>();
builder.Services.AddSingleton<ConfigurationValidationService>();
builder.Services.AddSingleton<ISimulationJobStore, InMemorySimulationJobStore>();

var app = builder.Build();

app.MapOpenApi();

app.MapHealthEndpoints();
app.MapModelsEndpoints();
app.MapPsychrometricsEndpoints();
app.MapConfigurationEndpoints();
app.MapSimulationEndpoints();

app.Run();

public partial class Program;
