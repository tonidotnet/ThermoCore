using ThermoCore.Core.Graph;

namespace ThermoCore.AWG.Topology;

public interface IAwgSystemGraphBuilder
{
    AwgBuiltSystem Build(AwgSystemConfiguration configuration, AwgInitialState initialState);
}
