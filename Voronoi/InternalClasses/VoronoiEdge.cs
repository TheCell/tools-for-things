using System.Drawing;

namespace Voronoi.InternalClasses;

public sealed class VoronoiEdge
{
    public required Point Start { get; init; }
    public Point? End { get; set; }

    public required Point LeftSite { get; init; }
    public required Point RightSite { get; init; }
}
