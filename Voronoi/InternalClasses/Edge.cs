namespace Voronoi.InternalClasses;

sealed class Edge
{
    public required Site Left { get; init; }
    public required Site Right { get; init; }

    public Point? Start { get; set; }
    public Point? End { get; set; }

    public bool Finished => Start.HasValue && End.HasValue;
}
