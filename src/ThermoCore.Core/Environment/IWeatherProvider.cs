namespace ThermoCore.Core.Environment;

/// <summary>Resolves weather at a simulation timestamp (docs/04_Simulation/28_WeatherModel.md §21).</summary>
public interface IWeatherProvider
{
    WeatherState GetState(DateTimeOffset timestampUtc);

    WeatherSourceMetadata Metadata { get; }
}
