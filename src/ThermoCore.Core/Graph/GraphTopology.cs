namespace ThermoCore.Core.Graph;

/// <summary>
/// Deterministic topological ordering for acyclic simulation graphs
/// (docs/04_Simulation/16_SimulationEngine.md §9).
/// </summary>
public static class GraphTopology
{
    public static IReadOnlyList<string> OrderComponentIds(SimulationGraph graph)
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
            if (string.Equals(connection.SourceComponentId, connection.TargetComponentId, StringComparison.Ordinal))
            {
                throw new SimulationGraphException(
                    $"Self-connection '{connection.Id}' is not supported in the acyclic engine.");
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
            var cyclic = componentIds
                .Where(id => !ordered.Contains(id, StringComparer.Ordinal))
                .OrderBy(id => id, StringComparer.Ordinal);
            throw new SimulationGraphException(
                "Graph contains one or more cycles; acyclic execution requires a DAG. " +
                $"Unresolved components: {string.Join(", ", cyclic)}.");
        }

        return ordered;
    }
}
