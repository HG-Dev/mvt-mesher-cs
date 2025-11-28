using LibTessDotNet;
using MvtMesherCore.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MvtMesherCore.LibTessDotNetIntegration;

public static class PolygonExtensions
{
    public static ContourVertex[] ToContour(this FloatPoints points)
    {
        return points.Select(p => new ContourVertex(new Vec3(p.X, p.Y, 0))).ToArray();
    }

    // public static IEnumerable<ContourVertex[]> ToContours(this IEnumerable<ReadOnlyPoints> unclosedRings)
    // {
    //     foreach (var ring in unclosedRings)
    //         yield return ring.ToContour();
    // }

    public static IEnumerable<ContourVertex[]> ToContours(this FloatPolygon polygon)
    {
        foreach (var polyline in polygon.AllRings)
            yield return polyline.UnclosedPoints.ToContour();
    }
}