using Microsoft.Extensions.Options;
using ThermoCore.Api.Services;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Persistence;
using ThermoCore.Web.Components;
using ThermoCore.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<ApiResourceLimits>(builder.Configuration.GetSection("ApiResourceLimits"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ApiResourceLimits>>().Value);
builder.Services.AddSingleton<IPsychrometricCalculator, PsychrometricCalculator>();
builder.Services.AddSingleton<PsychrometricApiService>();
builder.Services.AddSingleton<ConfigurationValidationService>();
builder.Services.AddThermoCorePersistence(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<ISimulationJobStore, InMemorySimulationJobStore>();
builder.Services.AddSingleton<SimulationResultQueryService>();
builder.Services.AddSingleton<IThermoCoreApiClient, InProcessThermoCoreApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapGet("/health/live", () => Results.Ok(new { status = "Live" }));
app.MapGet("/health/ready", (IThermoCoreStore store) =>
{
    try
    {
        store.EnsureCreated();
        _ = store.ListSimulationSummaries(1);
        return Results.Ok(new { status = "Ready", persistence = "ok" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "NotReady", detail = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
