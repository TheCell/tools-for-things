using System.Drawing;
using System.Drawing.Drawing2D;
using Voronoi.InternalClasses;

namespace Voronoi;

public class DrawImage
{
    public void GetImage(
        VoronoiDiagram voronoiDiagram,
        Point[] points,
        RectangleF? boundaries = null,
        float padding = 20f)
    {
        var drawingBounds = boundaries ?? GetBounds(voronoiDiagram, points);
        drawingBounds = ExpandBounds(drawingBounds, padding);

        var bitmapWidth = Math.Max(1, (int)Math.Ceiling(drawingBounds.Width));
        var bitmapHeight = Math.Max(1, (int)Math.Ceiling(drawingBounds.Height));

        using var bitmap = new Bitmap(bitmapWidth, bitmapHeight);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.White);

            using var borderPen = new Pen(Color.Gray, 1);
            using var edgePen = new Pen(Color.Black, 2);
            using var siteBrush = new SolidBrush(Color.Red);

            graphics.DrawRectangle(
                borderPen,
                padding,
                padding,
                drawingBounds.Width - padding * 2f,
                drawingBounds.Height - padding * 2f);

            // Draw Voronoi edges
            foreach (var edge in voronoiDiagram.Edges)
            {
                var start = edge.Start;

                if (edge.End.HasValue)
                {
                    var end = edge.End.Value;

                    graphics.DrawLine(
                        edgePen,
                        WorldToImage(start.X, drawingBounds.Left),
                        WorldToImage(start.Y, drawingBounds.Top),
                        WorldToImage(end.X, drawingBounds.Left),
                        WorldToImage(end.Y, drawingBounds.Top));
                }
            }

            // Draw sites
            foreach (var point in points)
            {
                graphics.FillEllipse(
                    siteBrush,
                    WorldToImage(point.X, drawingBounds.Left) - 4,
                    WorldToImage(point.Y, drawingBounds.Top) - 4,
                    8,
                    8);
            }

            // Draw Voronoi vertices
            foreach (var vertex in voronoiDiagram.Vertices)
            {
                graphics.FillEllipse(
                    Brushes.Blue,
                    WorldToImage(vertex.Position.X, drawingBounds.Left) - 3,
                    WorldToImage(vertex.Position.Y, drawingBounds.Top) - 3,
                    6,
                    6);
            }
        }

        bitmap.Save(
            "voronoi.png",
            System.Drawing.Imaging.ImageFormat.Png);
    }

    private static RectangleF GetBounds(VoronoiDiagram voronoiDiagram, Point[] points)
    {
        var hasPoints = false;
        double minX = 0;
        double minY = 0;
        double maxX = 0;
        double maxY = 0;

        void Include(double x, double y)
        {
            if (!hasPoints)
            {
                minX = maxX = x;
                minY = maxY = y;
                hasPoints = true;
                return;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        foreach (var point in points)
        {
            Include(point.X, point.Y);
        }

        foreach (var vertex in voronoiDiagram.Vertices)
        {
            Include(vertex.Position.X, vertex.Position.Y);
        }

        foreach (var edge in voronoiDiagram.Edges)
        {
            Include(edge.Start.X, edge.Start.Y);

            if (edge.End.HasValue)
            {
                Include(edge.End.Value.X, edge.End.Value.Y);
            }
        }

        if (!hasPoints)
        {
            return new RectangleF(0, 0, 100, 100);
        }

        var width = maxX - minX;
        var height = maxY - minY;

        if (width < 1)
        {
            width = 1;
        }

        if (height < 1)
        {
            height = 1;
        }

        return new RectangleF((float)minX, (float)minY, (float)width, (float)height);
    }

    private static RectangleF ExpandBounds(RectangleF bounds, float padding)
    {
        return new RectangleF(
            bounds.Left - padding,
            bounds.Top - padding,
            bounds.Width + padding * 2f,
            bounds.Height + padding * 2f);
    }

    private static float WorldToImage(double value, float origin)
    {
        return (float)value - origin;
    }
}
