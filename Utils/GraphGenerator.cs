using GraphTraversal.Models;

namespace GraphTraversal.Utils;

public static class GraphGenerator
{
    private static readonly Random rng = new();

    // Grafo simple de 8 nodos en forma de arbol
    public static Graph CreateSampleTree()
    {
        var g = new Graph();
        for (int i = 1; i <= 8; i++)
            g.AddNode(i, $"N{i}");

        g.AddEdge(1, 2); g.AddEdge(1, 3);
        g.AddEdge(2, 4); g.AddEdge(2, 5);
        g.AddEdge(3, 6); g.AddEdge(3, 7);
        g.AddEdge(4, 8);

        return g;
    }

    // Grafo denso con ciclos (9 nodos)
    public static Graph CreateSampleGraph()
    {
        var g = new Graph();
        for (int i = 0; i < 9; i++)
            g.AddNode(i, $"V{i}");

        g.AddEdge(0, 1); g.AddEdge(0, 2);
        g.AddEdge(1, 3); g.AddEdge(1, 4);
        g.AddEdge(2, 5); g.AddEdge(2, 6);
        g.AddEdge(3, 7); g.AddEdge(4, 7);
        g.AddEdge(5, 8); g.AddEdge(6, 8);
        g.AddEdge(7, 8); g.AddEdge(3, 4); // ciclo
        g.AddEdge(5, 6); // ciclo

        return g;
    }

    // Grafo aleatorio
    public static Graph CreateRandom(int nodeCount, double edgeProbability)
    {
        var g = new Graph();
        for (int i = 1; i <= nodeCount; i++)
            g.AddNode(i, $"N{i}");

        for (int i = 1; i <= nodeCount; i++)
            for (int j = i + 1; j <= nodeCount; j++)
                if (rng.NextDouble() < edgeProbability)
                    g.AddEdge(i, j);

        return g;
    }
}
