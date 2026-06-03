using System.Diagnostics;
using GraphTraversal.Models;

namespace GraphTraversal.Algorithms;

public class BFS
{
    /// <summary>
    /// Ejecuta BFS desde startId. Si onStep no es null, se invoca en cada paso
    /// permitiendo que la UI reaccione en tiempo real.
    /// </summary>
    public static TraversalResult Execute(Graph graph, int startId, Action<StepSnapshot>? onStep = null)
    {
        var sw = Stopwatch.StartNew();
        var result = new TraversalResult
        {
            StartNode = startId
        };
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        var levels = new Dictionary<int, int>();

        visited.Add(startId);
        queue.Enqueue(startId);
        levels[startId] = 0;
        result.VisitOrder.Add(startId);
        result.BfsLevels[startId] = 0;

        var initialStep = new StepSnapshot(
            startId,
            queue.ToList(),
            new HashSet<int>(visited),
            null,
            $"Iniciar BFS desde {graph.GetNode(startId)?.Label ?? $"N{startId}"}"
        );
        result.Steps.Add(initialStep);
        onStep?.Invoke(initialStep);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (!graph.AdjacencyList.ContainsKey(current))
                continue;

            foreach (int neighbor in graph.AdjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    levels[neighbor] = levels[current] + 1;
                    result.BfsLevels[neighbor] = levels[current] + 1;
                    result.VisitOrder.Add(neighbor);
                    result.EdgeTraversal.Add((current, neighbor));
                    result.TreeEdges.Add((current, neighbor));
                }
            }

            var step = new StepSnapshot(
                current,
                queue.ToList(),
                new HashSet<int>(visited),
                result.Steps.Count > 1 ? result.Steps[^1].CurrentNode : null,
                $"Desencolar {graph.GetNode(current)?.Label ?? $"N{current}"}  |  Nivel {levels.GetValueOrDefault(current, 0)}  |  Cola: [{string.Join(", ", queue.Select(n => graph.GetNode(n)?.Label ?? $"N{n}"))}]"
            );
            result.Steps.Add(step);
            onStep?.Invoke(step);
        }

        sw.Stop();
        result.DurationMs = sw.ElapsedMilliseconds;
        return result;
    }
}
