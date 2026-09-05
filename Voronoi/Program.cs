using System;
using System.Collections.Generic;
using System.Text;

namespace Voronoi;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, Voronoi!");

        var points = new[]
        {
            new Point(0, 0),
            new Point(600, 0),
            new Point(0, 600),
            new Point(600, 600),
            new Point(100, 100),
            new Point(120, 120),
            new Point(140, 140),
            new Point(160, 140),
            new Point(300, 150),
            new Point(200, 300),
            new Point(500, 250),
            new Point(400, 400)
        };

        var fortune = new FortuneVoronoi(points);

        var diagram = fortune.Compute();


        foreach (var vertex in diagram.Vertices)
        {
            Console.WriteLine(
                $"Vertex: {vertex.Position.X}, {vertex.Position.Y}");
        }

        foreach (var edge in diagram.Edges)
        {
            Console.WriteLine(
                $"{edge.LeftSite} <-> {edge.RightSite}: " +
                $"{edge.Start} -> {edge.End?.ToString() ?? "infinity"}");
        }

        var drawImage = new DrawImage();
        drawImage.GetImage(diagram, points);
    }
}
