using System.Diagnostics;
using GraphTraversal.Models;

namespace GraphTraversal.Algorithms;

public class DFS
{
    /// <summary>
    /// DFS Iterativo (con pila explícita). Si onStep no es null, se invoca en cada paso.
    /// </summary>
    public static TraversalResult ExecuteIterative(Graph graph, int startId, Action<StepSnapshot>? onStep = null)
    {
        var sw = Stopwatch.StartNew();
        var result = new TraversalResult
        {
            StartNode = startId
        };
        var visited = new HashSet<int>();
        var stack = new Stack<int>();

        stack.Push(startId);

        var initialStep = new StepSnapshot(
            startId,
            stack.ToList(),
            new HashSet<int>(visited),
            null,
            $"Iniciar DFS desde {graph.GetNode(startId)?.Label ?? $"N{startId}"}"
        );
        result.Steps.Add(initialStep);
        onStep?.Invoke(initialStep);

        while (stack.Count > 0)
        {
            int current = stack.Pop();

            if (visited.Contains(current))
            {
                var btStep = new StepSnapshot(
                    current,
                    stack.ToList(),
                    new HashSet<int>(visited),
                    result.Steps.Count > 0 ? result.Steps[^1].CurrentNode : null,
                    $"↩ Retroceder a {graph.GetNode(current)?.Label ?? $"N{current}"} (ya visitado)"
                );
                result.Steps.Add(btStep);
                onStep?.Invoke(btStep);
                continue;
            }

            visited.Add(current);
            result.VisitOrder.Add(current);

            if (!graph.AdjacencyList.ContainsKey(current))
                continue;

            var neighbors = graph.AdjacencyList[current].ToList();
            neighbors.Reverse();
            foreach (int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                    result.EdgeTraversal.Add((current, neighbor));
                    result.TreeEdges.Add((current, neighbor));
                }
                else
                {
                    if (!result.TreeEdges.Contains((neighbor, current)))
                        result.BacktrackEdges.Add((current, neighbor));
                }
            }

            var nodeLabel = graph.GetNode(current)?.Label ?? $"N{current}";
            var stackLabels = string.Join(", ", stack.Select(n => graph.GetNode(n)?.Label ?? $"N{n}"));
            var step = new StepSnapshot(
                current,
                stack.ToList(),
                new HashSet<int>(visited),
                result.Steps.Count > 1 ? result.Steps[^1].CurrentNode : null,
                visited.Count == 1
                    ? $"Visitar {nodeLabel}  |  Pila: [{stackLabels}]"
                    : $"Visitar {nodeLabel}, apilar vecinos  |  Pila: [{stackLabels}]"
            );
            result.Steps.Add(step);
            onStep?.Invoke(step);
        }

        sw.Stop();
        result.DurationMs = sw.ElapsedMilliseconds;
        return result;
    }

    /// <summary>
    /// DFS Recursivo. Si onStep no es null, se invoca en cada paso.
    /// </summary>
    public static TraversalResult ExecuteRecursive(Graph graph, int startId, Action<StepSnapshot>? onStep = null)
    {
        var sw = Stopwatch.StartNew();
        var result = new TraversalResult
        {
            StartNode = startId
        };
        var visited = new HashSet<int>();
        var callStack = new Stack<int>();

        void DfsRecursive(int current)
        {
            visited.Add(current);
            result.VisitOrder.Add(current);

            var entryStep = new StepSnapshot(
                current,
                callStack.Reverse().ToList(),
                new HashSet<int>(visited),
                result.Steps.Count > 0 ? result.Steps[^1].CurrentNode : null,
                $"→ Llamada recursiva: visitar {graph.GetNode(current)?.Label ?? $"N{current}"}"
            );
            result.Steps.Add(entryStep);
            onStep?.Invoke(entryStep);

            if (!graph.AdjacencyList.ContainsKey(current))
                return;

            foreach (int neighbor in graph.AdjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    callStack.Push(neighbor);
                    result.EdgeTraversal.Add((current, neighbor));
                    result.TreeEdges.Add((current, neighbor));
                    DfsRecursive(neighbor);
                    callStack.Pop();

                    var returnStep = new StepSnapshot(
                        current,
                        callStack.Reverse().ToList(),
                        new HashSet<int>(visited),
                        neighbor,
                        $"↩ Retorno: volver a {graph.GetNode(current)?.Label ?? $"N{current}"}"
                    );
                    result.Steps.Add(returnStep);
                    onStep?.Invoke(returnStep);
                }
                else
                {
                    if (!result.TreeEdges.Contains((neighbor, current)))
                        result.BacktrackEdges.Add((current, neighbor));
                }
            }
        }

        callStack.Push(startId);
        DfsRecursive(startId);
        callStack.Pop();

        sw.Stop();
        result.DurationMs = sw.ElapsedMilliseconds;
        return result;
    }
}
