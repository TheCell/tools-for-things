namespace Voronoi.InternalClasses;

sealed class CircleEvent
{
    public required double Y { get; init; }
    public required Point Center { get; init; }

    public required Arc Arc { get; init; }

    public bool Valid { get; set; } = true;
}
