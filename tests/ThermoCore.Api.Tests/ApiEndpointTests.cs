using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ThermoCore.Api.Contracts;
using ThermoCore.AWG.Configuration;

namespace ThermoCore.Api.Tests;

public class ApiEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
        Assert.Equal("0.1.0", body.ApplicationVersion);
    }

    [Fact]
    public async Task Models_ReturnsTopologyCatalog()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/models");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ModelCatalogResponse>();
        Assert.NotNull(body);
        Assert.Equal("awg-v3-mvp", body.TopologyId);
        Assert.Contains("dynamic-electrothermal-pv", body.ComponentModelIds);
    }

    [Fact]
    public async Task Psychrometrics_Calculate_ReturnsHumidityRatio()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/psychrometrics/calculate",
            new PsychrometricCalculateRequest
            {
                TemperatureC = 30.0,
                RelativeHumidityPercent = 50.0,
                AbsolutePressurePa = 101_325.0
            });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PsychrometricCalculateResponse>();
        Assert.NotNull(body);
        Assert.InRange(body.HumidityRatioKgPerKgDryAir, 0.012, 0.015);
        Assert.NotNull(body.DewPointTemperatureC);
    }

    [Fact]
    public async Task Configurations_Validate_AcceptsDefaultDocument()
    {
        var client = _factory.CreateClient();
        var document = AwgConfigurationLoader.CreateDefaultDocument();
        var json = AwgConfigurationLoader.SaveToJson(document);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/configurations/validate", content);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ConfigurationValidateResponse>();
        Assert.NotNull(body);
        Assert.True(body.IsValid);
    }

    [Fact]
    public async Task Simulations_CreateAndSummarize_Succeeds()
    {
        var client = _factory.CreateClient();
        var document = AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false);
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/simulations",
            new CreateSimulationRequest
            {
                Configuration = document,
                DurationSeconds = 3,
                TimeStepSeconds = 1
            });

        Assert.Equal(System.Net.HttpStatusCode.Accepted, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSimulationResponse>();
        Assert.NotNull(created);

        SimulationStatusResponse? status = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var statusResponse = await client.GetAsync($"/api/v1/simulations/{created.SimulationId}");
            statusResponse.EnsureSuccessStatusCode();
            status = await statusResponse.Content.ReadFromJsonAsync<SimulationStatusResponse>();
            if (status?.Status is "Completed" or "Failed" or "Cancelled")
            {
                break;
            }

            await Task.Delay(100);
        }

        Assert.NotNull(status);
        Assert.Equal("Completed", status.Status);

        var summaryResponse = await client.GetAsync($"/api/v1/simulations/{created.SimulationId}/summary");
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<SimulationSummaryResponse>();
        Assert.NotNull(summary);
        Assert.True(summary.Succeeded);
        Assert.True(summary.WaterBalancePassed);
        Assert.True(summary.EnergyBalancePassed);
        Assert.Equal(3, summary.CompletedSteps);
    }
}
