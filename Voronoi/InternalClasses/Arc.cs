namespace Voronoi.InternalClasses;

sealed class Arc
{
    public required Site Site { get; init; }

    public Arc? Prev { get; set; }
    public Arc? Next { get; set; }

    public CircleEvent? Event { get; set; }

    public Edge? LeftEdge { get; set; }
    public Edge? RightEdge { get; set; }
}
