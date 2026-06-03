namespace GraphTraversal.Models;

public class GraphNode
{
    public int Id { get; set; }
    public string Label { get; set; }
    public Point Position { get; set; } // para dibujar en el forms
    public bool IsSelected { get; set; }

    public GraphNode(int id, string label)
    {
        Id = id;
        Label = label;
        Position = Point.Empty;
    }

    public override string ToString() => Label;
}
