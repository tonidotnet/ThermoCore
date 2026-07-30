using Microsoft.Extensions.DependencyInjection;
using ThermoCore.Api.Contracts;
using ThermoCore.Api.Services;
using ThermoCore.AWG.Configuration;
using ThermoCore.Core.Psychrometrics;
using ThermoCore.Web.Services;

namespace ThermoCore.Web.Tests;

public class InProcessApiClientTests
{
    [Fact]
    public async Task Psychrometrics_And_ShortSimulation_WorkThroughClient()
    {
        await using var provider = BuildProvider();
        var client = provider.GetRequiredService<IThermoCoreApiClient>();

        var health = await client.GetHealthAsync();
        Assert.Equal("Healthy", health.Status);

        var psycho = await client.CalculatePsychrometricsAsync(new PsychrometricCalculateRequest
        {
            TemperatureC = 25,
            RelativeHumidityPercent = 50,
            AbsolutePressurePa = 101_325
        });
        Assert.True(psycho.HumidityRatioKgPerKgDryAir > 0);

        var created = await client.CreateSimulationAsync(new CreateSimulationRequest
        {
            Configuration = AwgConfigurationLoader.CreateDefaultDocument(enableElectricalSubsystem: false),
            DurationSeconds = 2,
            TimeStepSeconds = 1
        });

        SimulationStatusResponse? status = null;
        for (var i = 0; i < 50; i++)
        {
            status = await client.GetSimulationAsync(created.SimulationId);
            if (status?.Status is "Completed" or "Failed" or "Cancelled")
            {
                break;
            }

            await Task.Delay(100);
        }

        Assert.NotNull(status);
        Assert.Equal("Completed", status.Status);

        var summary = await client.GetSummaryAsync(created.SimulationId);
        Assert.NotNull(summary);
        Assert.True(summary.Succeeded);

        var series = await client.GetSeriesAsync(created.SimulationId, pageSize: 5);
        Assert.NotNull(series);
        Assert.True(series.TotalChannels > 0);

        var export = await client.ExportAsync(created.SimulationId, "json");
        Assert.NotNull(export);
        Assert.True(export.Value.Content.Length > 0);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(ApiResourceLimits.Default);
        services.AddSingleton<IPsychrometricCalculator, PsychrometricCalculator>();
        services.AddSingleton<PsychrometricApiService>();
        services.AddSingleton<ConfigurationValidationService>();
        services.AddSingleton<ISimulationJobStore, InMemorySimulationJobStore>();
        services.AddSingleton<SimulationResultQueryService>();
        services.AddSingleton<IThermoCoreApiClient, InProcessThermoCoreApiClient>();
        return services.BuildServiceProvider();
    }
}
