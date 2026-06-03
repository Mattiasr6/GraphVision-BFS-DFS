namespace GraphTraversal.Models;

/// <summary>
/// Captura inmutable del estado de la estructura de datos en un paso del recorrido.
/// </summary>
/// <param name="CurrentNode">Nodo siendo procesado en este paso.</param>
/// <param name="StructureState">Estado actual de la cola (BFS) o pila (DFS).</param>
/// <param name="Visited">Conjunto de nodos visitados hasta este paso.</param>
/// <param name="PreviousNode">Nodo desde el que se llegó, si aplica.</param>
/// <param name="Description">Descripción textual del paso.</param>
public record StepSnapshot(
    int CurrentNode,
    IReadOnlyList<int> StructureState,
    IReadOnlySet<int> Visited,
    int? PreviousNode,
    string Description
);

public class TraversalResult
{
    public List<int> VisitOrder { get; init; } = new();
    public List<(int From, int To)> EdgeTraversal { get; init; } = new();
    public List<(int From, int To)> BacktrackEdges { get; init; } = new();
    public long DurationMs { get; set; }

    public int NodesVisited => VisitOrder.Count;
    public int EdgeCount => EdgeTraversal.Count;

    /// <summary>Nivel BFS de cada nodo (distancia desde el origen).</summary>
    public Dictionary<int, int> BfsLevels { get; init; } = new();

    /// <summary>Snapshots para animación paso a paso.</summary>
    public List<StepSnapshot> Steps { get; init; } = new();

    /// <summary>Árbol de expansión del recorrido.</summary>
    public HashSet<(int From, int To)> TreeEdges { get; init; } = new();

    /// <summary>Nodo origen.</summary>
    public int StartNode { get; init; }
}
