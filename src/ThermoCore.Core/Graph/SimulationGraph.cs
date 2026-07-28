using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Core.Graph;

public sealed class SimulationGraphException : Exception
{
    public SimulationGraphException(string message)
        : base(message)
    {
    }

    public SimulationGraphException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed record GraphValidationResult
{
    public required bool IsValid { get; init; }

    public required IReadOnlyList<SimulationDiagnostic> Diagnostics { get; init; }
}

public sealed class SimulationGraph
{
    private readonly Dictionary<string, ISimulationComponent> _components;
    private readonly List<PhysicalConnection> _connections;

    public SimulationGraph(
        IEnumerable<ISimulationComponent> components,
        IEnumerable<PhysicalConnection> connections)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(connections);

        _components = new Dictionary<string, ISimulationComponent>(StringComparer.Ordinal);
        foreach (var component in components)
        {
            ArgumentNullException.ThrowIfNull(component);
            if (!_components.TryAdd(component.Id, component))
            {
                throw new SimulationGraphException($"Duplicate component id '{component.Id}'.");
            }
        }

        _connections = connections.ToList();
    }

    public IReadOnlyCollection<ISimulationComponent> Components => _components.Values;

    public IReadOnlyCollection<PhysicalConnection> Connections => _connections;

    public ISimulationComponent GetComponent(string componentId)
    {
        if (!_components.TryGetValue(componentId, out var component))
        {
            throw new SimulationGraphException($"Unknown component id '{componentId}'.");
        }

        return component;
    }

    public IPhysicalPort GetPort(string componentId, string portId)
    {
        var component = GetComponent(componentId);
        var port = component.Ports.FirstOrDefault(p => string.Equals(p.Id, portId, StringComparison.Ordinal));
        if (port is null)
        {
            throw new SimulationGraphException($"Unknown port '{portId}' on component '{componentId}'.");
        }

        return port;
    }

    public GraphValidationResult Validate()
    {
        var diagnostics = new List<SimulationDiagnostic>();
        var connectionIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var connection in _connections)
        {
            if (!connectionIds.Add(connection.Id))
            {
                diagnostics.Add(Error("GRAPH.DUPLICATE_CONNECTION", $"Duplicate connection id '{connection.Id}'."));
                continue;
            }

            if (!_components.ContainsKey(connection.SourceComponentId))
            {
                diagnostics.Add(Error(
                    "GRAPH.UNKNOWN_SOURCE_COMPONENT",
                    $"Connection '{connection.Id}' references unknown source component '{connection.SourceComponentId}'.",
                    connection.SourceComponentId));
                continue;
            }

            if (!_components.ContainsKey(connection.TargetComponentId))
            {
                diagnostics.Add(Error(
                    "GRAPH.UNKNOWN_TARGET_COMPONENT",
                    $"Connection '{connection.Id}' references unknown target component '{connection.TargetComponentId}'.",
                    connection.TargetComponentId));
                continue;
            }

            IPhysicalPort sourcePort;
            IPhysicalPort targetPort;
            try
            {
                sourcePort = GetPort(connection.SourceComponentId, connection.SourcePortId);
                targetPort = GetPort(connection.TargetComponentId, connection.TargetPortId);
            }
            catch (SimulationGraphException ex)
            {
                diagnostics.Add(Error("GRAPH.UNKNOWN_PORT", ex.Message));
                continue;
            }

            if (sourcePort.Direction is not (PortDirection.Output or PortDirection.Bidirectional))
            {
                diagnostics.Add(Error(
                    "GRAPH.INVALID_SOURCE_DIRECTION",
                    $"Connection '{connection.Id}' source port '{sourcePort.Id}' must be Output or Bidirectional.",
                    sourcePort.ComponentId,
                    sourcePort.Id));
            }

            if (targetPort.Direction is not (PortDirection.Input or PortDirection.Bidirectional))
            {
                diagnostics.Add(Error(
                    "GRAPH.INVALID_TARGET_DIRECTION",
                    $"Connection '{connection.Id}' target port '{targetPort.Id}' must be Input or Bidirectional.",
                    targetPort.ComponentId,
                    targetPort.Id));
            }

            if (sourcePort.Domain != targetPort.Domain)
            {
                diagnostics.Add(Error(
                    "GRAPH.DOMAIN_MISMATCH",
                    $"Connection '{connection.Id}' domains differ: {sourcePort.Domain} → {targetPort.Domain}.",
                    sourcePort.ComponentId,
                    sourcePort.Id));
            }
        }

        var connectedRequiredPorts = new HashSet<(string ComponentId, string PortId)>();
        foreach (var connection in _connections)
        {
            connectedRequiredPorts.Add((connection.SourceComponentId, connection.SourcePortId));
            connectedRequiredPorts.Add((connection.TargetComponentId, connection.TargetPortId));
        }

        foreach (var component in _components.Values)
        {
            foreach (var port in component.Ports.Where(p => p.IsRequired))
            {
                if (!connectedRequiredPorts.Contains((component.Id, port.Id)))
                {
                    diagnostics.Add(Error(
                        "GRAPH.REQUIRED_PORT_UNCONNECTED",
                        $"Required port '{port.Id}' on component '{component.Id}' is not connected.",
                        component.Id,
                        port.Id));
                }
            }
        }

        return new GraphValidationResult
        {
            IsValid = diagnostics.Count == 0,
            Diagnostics = diagnostics
        };
    }

    private static SimulationDiagnostic Error(
        string code,
        string message,
        string? componentId = null,
        string? portId = null)
        => new()
        {
            Code = code,
            Severity = DiagnosticSeverity.Error,
            Message = message,
            ComponentId = componentId,
            PortId = portId
        };
}
