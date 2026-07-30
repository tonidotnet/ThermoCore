using System.Text.Json;
using System.Text.Json.Serialization;
using ThermoCore.AWG.Topology;

namespace ThermoCore.AWG.Configuration;

/// <summary>Root JSON document for AWG configuration loading (APP-002).</summary>
public sealed record AwgConfigurationDocument
{
    public required AwgSystemConfiguration System { get; init; }

    public required AwgInitialState InitialState { get; init; }
}
