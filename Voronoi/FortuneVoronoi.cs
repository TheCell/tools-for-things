using Voronoi.InternalClasses;

namespace Voronoi;

public readonly record struct Point(double X, double Y);

public sealed class FortuneVoronoi
{
    private const double Epsilon = 1e-10;

    private readonly List<Site> _sites;
    private readonly List<Edge> _edges = [];
    private readonly List<VoronoiVertex> _vertices = [];

    private Arc? _beachLine;
    private double _sweepY;

    public FortuneVoronoi(IEnumerable<Point> points)
    {
        _sites = points
            .Select((p, i) => new Site
            {
                Point = p,
                Index = i
            })
            .OrderBy(p => p.Point.Y)
            .ThenBy(p => p.Point.X)
            .ToList();
    }

    public VoronoiDiagram Compute()
    {
        if (_sites.Count == 0)
        {
            return new VoronoiDiagram();
        }

        if (_sites.Count == 1)
        {
            return new VoronoiDiagram();
        }

        var events = new PriorityQueue<object, EventKey>();

        foreach (var site in _sites)
        {
            events.Enqueue(
                site,
                new EventKey(site.Point.Y, site.Point.X, 0));
        }

        while (events.Count > 0)
        {
            var item = events.Dequeue();

            switch (item)
            {
                case Site site:
                    _sweepY = site.Point.Y;
                    HandleSiteEvent(site, events);
                    break;

                case CircleEvent circle:
                    if (!circle.Valid)
                        continue;

                    _sweepY = circle.Y;
                    HandleCircleEvent(circle, events);
                    break;
            }
        }

        // Finish all edges that still extend to infinity.
        FinishInfiniteEdges();

        var diagram = new VoronoiDiagram();

        diagram.Vertices.AddRange(_vertices);

        diagram.Edges.AddRange(
            _edges
                .Where(e => e.Start.HasValue)
                .Select(e => new VoronoiEdge
                {
                    Start = e.Start!.Value,
                    End = e.End,
                    LeftSite = e.Left.Point,
                    RightSite = e.Right.Point
                }));

        return diagram;
    }

    // ---------------------------------------------------------------------
    // Site events
    // ---------------------------------------------------------------------

    private void HandleSiteEvent(
        Site site,
        PriorityQueue<object, EventKey> events)
    {
        if (_beachLine == null)
        {
            _beachLine = new Arc
            {
                Site = site
            };

            return;
        }

        var arc = FindArcAbove(site.Point.X);

        if (arc.Event != null)
        {
            InvalidateCircleEvent(arc);
        }

        // Split:
        //
        //        A
        //
        // into
        //
        //        A
        //        B
        //        A
        //
        var left = new Arc
        {
            Site = arc.Site
        };

        var middle = new Arc
        {
            Site = site
        };

        var right = new Arc
        {
            Site = arc.Site
        };

        left.Prev = arc.Prev;
        left.Next = middle;

        middle.Prev = left;
        middle.Next = right;

        right.Prev = middle;
        right.Next = arc.Next;

        if (left.Prev != null)
            left.Prev.Next = left;

        if (right.Next != null)
            right.Next.Prev = right;

        if (ReferenceEquals(_beachLine, arc))
            _beachLine = left;

        var edgeLeft = CreateEdge(arc.Site, site);
        var edgeRight = CreateEdge(site, arc.Site);

        left.RightEdge = edgeLeft;
        middle.LeftEdge = edgeLeft;

        middle.RightEdge = edgeRight;
        right.LeftEdge = edgeRight;

        CheckCircleEvent(left, events);
        CheckCircleEvent(right, events);
    }

    // ---------------------------------------------------------------------
    // Circle events
    // ---------------------------------------------------------------------

    private void HandleCircleEvent(
        CircleEvent circle,
        PriorityQueue<object, EventKey> events)
    {
        var arc = circle.Arc;

        if (!circle.Valid)
            return;

        var left = arc.Prev;
        var right = arc.Next;

        if (left == null || right == null)
            return;

        // The vertex is the circumcenter.
        var vertex = circle.Center;

        _vertices.Add(new VoronoiVertex
        {
            Position = vertex
        });

        // The edges terminating here are the boundaries between:
        //
        // left  <-> arc
        // arc   <-> right
        //
        FinishEdge(arc.LeftEdge, vertex);
        FinishEdge(arc.RightEdge, vertex);

        if (left.Event != null)
            InvalidateCircleEvent(left);

        if (right.Event != null)
            InvalidateCircleEvent(right);

        // Remove the disappearing arc.
        left.Next = right;
        right.Prev = left;

        // Create the new edge between left and right.
        var newEdge = CreateEdge(left.Site, right.Site);

        newEdge.Start = vertex;

        left.RightEdge = newEdge;
        right.LeftEdge = newEdge;

        CheckCircleEvent(left, events);
        CheckCircleEvent(right, events);
    }

    // ---------------------------------------------------------------------
    // Beach line
    // ---------------------------------------------------------------------

    private Arc FindArcAbove(double x)
    {
        var arc = _beachLine!;

        while (arc.Next != null)
        {
            var breakpoint = GetBreakpointX(
                arc.Site.Point,
                arc.Next.Site.Point,
                _sweepY);

            if (x < breakpoint)
                break;

            arc = arc.Next;
        }

        return arc;
    }

    /// <summary>
    /// X coordinate where the parabolas belonging to p and q intersect
    /// at the current sweepline.
    ///
    /// The sweepline moves upward, matching Fortune's paper.
    /// </summary>
    private static double GetBreakpointX(
        Point p,
        Point q,
        double sweepY)
    {
        // One of the parabolas has its focus exactly on the sweep line.
        if (Math.Abs(p.Y - sweepY) < Epsilon)
            return p.X;

        if (Math.Abs(q.Y - sweepY) < Epsilon)
            return q.X;

        // Same y-coordinate: the breakpoint is simply their midpoint.
        if (Math.Abs(p.Y - q.Y) < Epsilon)
            return (p.X + q.X) * 0.5;

        //
        // Parabola equation:
        //
        // (x - px)^2 + (y - py)^2 = (y - sweepY)^2
        //
        // Solving the two parabola equations gives a quadratic in x.
        //

        var dp = 2.0 * (p.Y - sweepY);
        var dq = 2.0 * (q.Y - sweepY);

        var a = 1.0 / dp - 1.0 / dq;

        var b =
            -2.0 * p.X / dp +
             2.0 * q.X / dq;

        var c =
            (p.X * p.X + p.Y * p.Y - sweepY * sweepY) / dp -
            (q.X * q.X + q.Y * q.Y - sweepY * sweepY) / dq;

        if (Math.Abs(a) < Epsilon)
        {
            if (Math.Abs(b) < Epsilon)
                return (p.X + q.X) * 0.5;

            return -c / b;
        }

        var discriminant = b * b - 4.0 * a * c;

        if (discriminant < 0)
            discriminant = 0;

        var sqrt = Math.Sqrt(discriminant);

        var x1 = (-b - sqrt) / (2.0 * a);
        var x2 = (-b + sqrt) / (2.0 * a);

        // For an upward-moving sweepline:
        //
        // lower site -> right breakpoint
        // higher site -> left breakpoint
        //
        return p.Y < q.Y
            ? Math.Max(x1, x2)
            : Math.Min(x1, x2);
    }

    // ---------------------------------------------------------------------
    // Circle event detection
    // ---------------------------------------------------------------------

    private void CheckCircleEvent(
        Arc arc,
        PriorityQueue<object, EventKey> events)
    {
        if (arc.Prev == null || arc.Next == null)
            return;

        if (arc.Event != null)
            InvalidateCircleEvent(arc);

        var a = arc.Prev.Site.Point;
        var b = arc.Site.Point;
        var c = arc.Next.Site.Point;

        // Collinear points cannot produce a circle event.
        var cross = Cross(
            new Point(b.X - a.X, b.Y - a.Y),
            new Point(c.X - b.X, c.Y - b.Y));

        // With an upward-moving sweep, only counter-clockwise triples
        // produce a valid disappearing arc.
        if (cross <= Epsilon)
            return;

        if (!TryGetCircumcircle(
                a,
                b,
                c,
                out var center,
                out var radius))
        {
            return;
        }

        // Fortune's sweep moves upward, so the event is at the TOP of
        // the circumcircle.
        var eventY = center.Y + radius;

        // The event must be in the future.
        if (eventY < _sweepY - Epsilon)
            return;

        var circle = new CircleEvent
        {
            Y = eventY,
            Center = center,
            Arc = arc
        };

        arc.Event = circle;

        events.Enqueue(
            circle,
            new EventKey(eventY, center.X, 1));
    }

    private static bool TryGetCircumcircle(
        Point a,
        Point b,
        Point c,
        out Point center,
        out double radius)
    {
        var d =
            2.0 *
            (a.X * (b.Y - c.Y) +
             b.X * (c.Y - a.Y) +
             c.X * (a.Y - b.Y));

        if (Math.Abs(d) < Epsilon)
        {
            center = default;
            radius = 0;
            return false;
        }

        var a2 = a.X * a.X + a.Y * a.Y;
        var b2 = b.X * b.X + b.Y * b.Y;
        var c2 = c.X * c.X + c.Y * c.Y;

        var ux =
            (a2 * (b.Y - c.Y) +
             b2 * (c.Y - a.Y) +
             c2 * (a.Y - b.Y)) / d;

        var uy =
            (a2 * (c.X - b.X) +
             b2 * (a.X - c.X) +
             c2 * (b.X - a.X)) / d;

        center = new Point(ux, uy);

        radius = Distance(center, a);

        return true;
    }

    private void InvalidateCircleEvent(Arc arc)
    {
        if (arc.Event == null)
            return;

        arc.Event.Valid = false;
        arc.Event = null;
    }

    // ---------------------------------------------------------------------
    // Edges
    // ---------------------------------------------------------------------

    private Edge CreateEdge(Site left, Site right)
    {
        var edge = new Edge
        {
            Left = left,
            Right = right
        };

        _edges.Add(edge);

        return edge;
    }

    private static void FinishEdge(
        Edge? edge,
        Point point)
    {
        if (edge == null)
            return;

        if (!edge.Start.HasValue)
        {
            edge.Start = point;
        }
        else if (!edge.End.HasValue)
        {
            edge.End = point;
        }
    }

    /// <summary>
    /// Edges with only one endpoint continue to infinity.
    ///
    /// We keep them as half-lines by leaving End == null.
    /// </summary>
    private void FinishInfiniteEdges()
    {
        foreach (var edge in _edges)
        {
            if (!edge.Start.HasValue)
                continue;

            // Nothing else is necessary here because VoronoiEdge.End == null
            // explicitly represents an infinite ray.
        }
    }

    // ---------------------------------------------------------------------
    // Geometry
    // ---------------------------------------------------------------------

    private static double Cross(Point a, Point b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    private static double Distance(Point a, Point b)
    {
        return Math.Sqrt(
            (a.X - b.X) * (a.X - b.X) +
            (a.Y - b.Y) * (a.Y - b.Y));
    }

    // ---------------------------------------------------------------------
    // Priority queue ordering
    // ---------------------------------------------------------------------

    private readonly record struct EventKey(
        double Y,
        double X,
        int Type) : IComparable<EventKey>
    {
        public int CompareTo(EventKey other)
        {
            var result = Y.CompareTo(other.Y);

            if (result != 0)
                return result;

            result = X.CompareTo(other.X);

            if (result != 0)
                return result;

            return Type.CompareTo(other.Type);
        }
    }
}