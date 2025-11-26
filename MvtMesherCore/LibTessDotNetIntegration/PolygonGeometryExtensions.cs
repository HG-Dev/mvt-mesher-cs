using LibTessDotNet;
using MvtMesherCore.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MvtMesherCore.LibTessDotNetIntegration;

public static class PolygonExtensions
{
    public static ContourVertex[] ToContour(this ReadOnlyPoints points)
    {
        return points.Select(p => new ContourVertex(new Vec3(p.X, p.Y, 0))).ToArray();
    }

    public static IEnumerable<ContourVertex[]> ToContours(this ReadOnlyPolylines polylines)
    {
        foreach (var polyline in polylines)
            yield return polyline.ToContour();
    }

    public static IEnumerable<ContourVertex[]> ToContours(this MvtMesherCore.Collections.Polygon polygon)
    {
        foreach (var polyline in polygon.AllRings)
            yield return polyline.ToContour();
    }

    // /// <summary>
    // /// One feature may have multiple polygons (each with exterior and interior rings).
    // /// </summary>
    // public static IEnumerable<ContourVertex[]> ToContours(this ReadOnlyPolygons polygons)
    // {
    //     foreach (var polygon in polygons)
    //         foreach (var contour in polygon.ToContours())
    //             yield return contour;
    // }
}