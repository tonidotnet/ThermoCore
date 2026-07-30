using ThermoCore.Core.Environment;

namespace ThermoCore.AWG.Topology;

public interface IAwgSystemGraphBuilder
{
    AwgBuiltSystem Build(
        AwgSystemConfiguration configuration,
        AwgInitialState initialState,
        IWeatherProvider? weatherProvider = null);
}
