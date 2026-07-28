namespace ThermoCore.Core.Graph;

/// <summary>
/// Deterministic topological helpers for acyclic and torn cyclic graphs
/// (docs/04_Simulation/16_SimulationEngine.md §9–§11).
/// </summary>
public static class GraphTopology
{
    public static bool HasCycle(SimulationGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return !TryOrderComponentIds(graph, ignoredConnectionIds: null, out _);
    }

    public static IReadOnlyList<string> GetCyclicComponentIds(SimulationGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (TryOrderComponentIds(graph, ignoredConnectionIds: null, out _))
        {
            return Array.Empty<string>();
        }

        // Re-run Kahn to identify unresolved nodes.
        var componentIds = graph.Components.Select(c => c.Id).ToList();
        var indegree = componentIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        var successors = componentIds.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);

        foreach (var connection in graph.Connections)
        {
            successors[connection.SourceComponentId].Add(connection.TargetComponentId);
            indegree[connection.TargetComponentId]++;
        }

        var queue = new SortedSet<string>(
            indegree.Where(p => p.Value == 0).Select(p => p.Key),
            StringComparer.Ordinal);
        var resolved = new HashSet<string>(StringComparer.Ordinal);
        while (queue.Count > 0)
        {
            var next = queue.Min!;
            queue.Remove(next);
            resolved.Add(next);
            foreach (var successor in successors[next].Distinct(StringComparer.Ordinal))
            {
                indegree[successor]--;
                if (indegree[successor] == 0)
                {
                    queue.Add(successor);
                }
            }
        }

        return componentIds
            .Where(id => !resolved.Contains(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> OrderComponentIds(SimulationGraph graph)
    {
        if (!TryOrderComponentIds(graph, ignoredConnectionIds: null, out var ordered))
        {
            var cyclic = GetCyclicComponentIds(graph);
            throw new SimulationGraphException(
                "Graph contains one or more cycles; acyclic execution requires a DAG. " +
                $"Unresolved components: {string.Join(", ", cyclic)}.");
        }

        return ordered;
    }

    public static IReadOnlyList<string> OrderComponentIdsIgnoringConnections(
        SimulationGraph graph,
        IEnumerable<string> ignoredConnectionIds)
    {
        ArgumentNullException.ThrowIfNull(ignoredConnectionIds);
        if (!TryOrderComponentIds(graph, ignoredConnectionIds.ToHashSet(StringComparer.Ordinal), out var ordered))
        {
            var cyclic = GetCyclicComponentIds(graph);
            throw new SimulationGraphException(
                "Graph remains cyclic after tearing configured loop connections. " +
                $"Unresolved components: {string.Join(", ", cyclic)}.");
        }

        return ordered;
    }

    public static bool TryOrderComponentIds(
        SimulationGraph graph,
        ISet<string>? ignoredConnectionIds,
        out IReadOnlyList<string> orderedIds)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var componentIds = graph.Components
            .Select(c => c.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var successors = componentIds.ToDictionary(
            id => id,
            _ => new List<string>(),
            StringComparer.Ordinal);

        var indegree = componentIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

        foreach (var connection in graph.Connections)
        {
            if (ignoredConnectionIds is not null
                && ignoredConnectionIds.Contains(connection.Id))
            {
                continue;
            }

            if (string.Equals(connection.SourceComponentId, connection.TargetComponentId, StringComparison.Ordinal))
            {
                orderedIds = Array.Empty<string>();
                return false;
            }

            successors[connection.SourceComponentId].Add(connection.TargetComponentId);
            indegree[connection.TargetComponentId]++;
        }

        foreach (var id in componentIds)
        {
            successors[id] = successors[id]
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
        }

        var queue = new SortedSet<string>(
            indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key),
            StringComparer.Ordinal);

        var ordered = new List<string>(componentIds.Count);
        while (queue.Count > 0)
        {
            var next = queue.Min!;
            queue.Remove(next);
            ordered.Add(next);

            foreach (var successor in successors[next])
            {
                indegree[successor]--;
                if (indegree[successor] == 0)
                {
                    queue.Add(successor);
                }
            }
        }

        if (ordered.Count != componentIds.Count)
        {
            orderedIds = Array.Empty<string>();
            return false;
        }

        orderedIds = ordered;
        return true;
    }
}
