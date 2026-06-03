namespace GraphTraversal.Models;

public class Graph
{
    public Dictionary<int, GraphNode> Nodes { get; } = new();
    public Dictionary<int, List<int>> AdjacencyList { get; } = new();
    private int _nextId = 1;

    public int GetNextAvailableId()
    {
        while (Nodes.ContainsKey(_nextId))
            _nextId++;
        return _nextId;
    }

    public void AddNode(int id, string label)
    {
        if (!Nodes.ContainsKey(id))
        {
            Nodes[id] = new GraphNode(id, label);
            AdjacencyList[id] = new List<int>();
        }
    }

    public int AddNode(string label)
    {
        int id = GetNextAvailableId();
        AddNode(id, label);
        return id;
    }

    public void RemoveNode(int id)
    {
        if (!Nodes.ContainsKey(id)) return;

        // Eliminar aristas hacia este nodo
        foreach (var kv in AdjacencyList)
        {
            kv.Value.Remove(id);
        }
        AdjacencyList.Remove(id);
        Nodes.Remove(id);

        // Reajustar _nextId si es necesario
        if (id < _nextId)
            _nextId = id;
    }

    public void AddEdge(int from, int to)
    {
        if (!AdjacencyList.ContainsKey(from) || !AdjacencyList.ContainsKey(to))
            return;

        if (!AdjacencyList[from].Contains(to))
            AdjacencyList[from].Add(to);

        if (!AdjacencyList[to].Contains(from))
            AdjacencyList[to].Add(from);
    }

    public void RemoveEdge(int from, int to)
    {
        if (AdjacencyList.ContainsKey(from))
            AdjacencyList[from].Remove(to);
        if (AdjacencyList.ContainsKey(to))
            AdjacencyList[to].Remove(from);
    }

    public GraphNode? GetNode(int id) =>
        Nodes.TryGetValue(id, out var node) ? node : null;

    public int NodeCount => Nodes.Count;
    public int EdgeCount
    {
        get
        {
            int count = 0;
            foreach (var kv in AdjacencyList)
                count += kv.Value.Count;
            return count / 2; // no dirigido
        }
    }

    // Layout circular
    public void CalculateCircularLayout(Point center, int radius)
    {
        int i = 0;
        foreach (var node in Nodes.Values)
        {
            double angle = 2 * Math.PI * i / Nodes.Count;
            node.Position = new Point(
                center.X + (int)(radius * Math.Cos(angle)),
                center.Y + (int)(radius * Math.Sin(angle))
            );
            i++;
        }
    }

    // Layout jerárquico (tipo árbol) basado en BFS desde el primer nodo
    public void CalculateTreeLayout(Point center, int hSpacing, int vSpacing)
    {
        if (NodeCount == 0) return;

        int rootId = Nodes.Keys.First();
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        var levels = new Dictionary<int, int>();

        visited.Add(rootId);
        queue.Enqueue(rootId);
        levels[rootId] = 0;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (int neighbor in AdjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    levels[neighbor] = levels[current] + 1;
                }
            }
        }

        int maxLevel = levels.Values.Count > 0 ? levels.Values.Max() : 0;
        var levelCount = new Dictionary<int, int>();
        var levelIndex = new Dictionary<int, int>();

        foreach (var kv in levels.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key))
        {
            levelCount.TryGetValue(kv.Value, out int count);
            levelCount[kv.Value] = count + 1;
            levelIndex[kv.Key] = count;
        }

        foreach (var node in Nodes.Values)
        {
            if (!levels.ContainsKey(node.Id)) continue;
            int lvl = levels[node.Id];
            int count = levelCount[lvl];
            int idx = levelIndex[node.Id];
            int y = center.Y + lvl * vSpacing - (maxLevel * vSpacing / 2);
            int x = center.X + (int)((idx - (count - 1) / 2.0) * hSpacing);
            node.Position = new Point((int)x, y);
        }
    }

    // Layout aleatorio con separación mínima
    public void CalculateRandomLayout(Point center, int spread)
    {
        var rng = new Random(42);
        foreach (var node in Nodes.Values)
        {
            node.Position = new Point(
                center.X + rng.Next(-spread, spread),
                center.Y + rng.Next(-spread, spread)
            );
        }
    }

    /// <summary>
    /// Algoritmo Force-Directed (Fruchterman-Reingold simplificado) en 2D.
    /// Distribuye los nodos minimizando cruces mediante fuerzas de atracción
    /// (solo entre nodos conectados) y repulsión (entre todos los pares).
    /// </summary>
    public void CalculateForceDirectedLayout(Point center, int width, int height, int iterations = 100)
    {
        if (NodeCount == 0) return;

        var rng = new Random(42);
        var pos = new Dictionary<int, PointF>();
        var disp = new Dictionary<int, PointF>();

        // Inicializar posiciones aleatorias dentro del área
        foreach (var node in Nodes.Values)
        {
            pos[node.Id] = new PointF(
                center.X + rng.Next(-width / 3, width / 3),
                center.Y + rng.Next(-height / 3, height / 3)
            );
        }

        float area = width * height;
        float k = MathF.Sqrt(area / NodeCount); // constante de resorte
        float dt = 0.95f; // enfriamiento

        for (int iter = 0; iter < iterations; iter++)
        {
            // Calcular fuerzas de repulsión (todos los pares)
            foreach (var v in Nodes.Keys)
            {
                disp[v] = PointF.Empty;

                if (!pos.ContainsKey(v)) continue;

                foreach (var u in Nodes.Keys)
                {
                    if (u == v) continue;
                    if (!pos.ContainsKey(u)) continue;

                    float dx = pos[v].X - pos[u].X;
                    float dy = pos[v].Y - pos[u].Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist < 0.1f) dist = 0.1f; // evitar división por cero

                    // Fuerza de repulsión: Fr = k^2 / d
                    float repForce = (k * k) / dist;
                    disp[v] = new PointF(
                        disp[v].X + (dx / dist) * repForce,
                        disp[v].Y + (dy / dist) * repForce
                    );
                }
            }

            // Calcular fuerzas de atracción (solo para aristas)
            foreach (var kv in AdjacencyList)
            {
                int v = kv.Key;
                if (!pos.ContainsKey(v)) continue;

                foreach (int u in kv.Value)
                {
                    if (!pos.ContainsKey(u)) continue;

                    float dx = pos[v].X - pos[u].X;
                    float dy = pos[v].Y - pos[u].Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist < 0.1f) dist = 0.1f;

                    // Fuerza de atracción: Fa = d^2 / k
                    float attForce = (dist * dist) / k;
                    disp[v] = new PointF(
                        disp[v].X - (dx / dist) * attForce,
                        disp[v].Y - (dy / dist) * attForce
                    );
                    disp[u] = new PointF(
                        disp[u].X + (dx / dist) * attForce,
                        disp[u].Y + (dy / dist) * attForce
                    );
                }
            }

            // Aplicar desplazamientos con enfriamiento progresivo
            float t = 1.0f - (float)iter / iterations; // temperatura
            float maxDisp = 10f * t; // límite de desplazamiento por iteración
            if (maxDisp < 1f) maxDisp = 1f;

            foreach (var v in Nodes.Keys)
            {
                if (!pos.ContainsKey(v)) continue;

                float dx = disp[v].X * t * dt;
                float dy = disp[v].Y * t * dt;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d > maxDisp)
                {
                    dx = dx / d * maxDisp;
                    dy = dy / d * maxDisp;
                }

                pos[v] = new PointF(pos[v].X + dx, pos[v].Y + dy);

                // Mantener dentro del área
                float margin = 30;
                pos[v] = new PointF(
                    Math.Clamp(pos[v].X, center.X - width / 2f + margin, center.X + width / 2f - margin),
                    Math.Clamp(pos[v].Y, center.Y - height / 2f + margin, center.Y + height / 2f - margin)
                );
            }
        }

        // Asignar posiciones finales
        foreach (var node in Nodes.Values)
        {
            if (pos.TryGetValue(node.Id, out var p))
                node.Position = new Point((int)p.X, (int)p.Y);
        }
    }
}
