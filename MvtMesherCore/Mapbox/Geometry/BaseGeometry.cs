using System;
using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

// TODO: Optimize this by converting its underlying data into a 1D array of float and return things as slices of array
// Duck typing: look at slice counts to figure out what the type is
public abstract class BaseGeometry
{
    public readonly GeometryType DeclaredType;
    public abstract bool Parsed { get; }
    public virtual int MajorElementCount => 0;

    //public abstract ReadOnlyPoints Points { get; }
    // public IReadOnlyList<Polyline> Polylines { get; private set; }
    // public IReadOnlyList<Polygon> Polygons { get; private set; }

    public BaseGeometry(GeometryType declaredType)
    {
        DeclaredType = declaredType;
        // _commands = field;
        // // We can expect there to be a maximum (bytes / 4) points;
        // // One-point geometry consists of 4-5 bytes.
        // // TODO: Test this with point @ origin
        // // TODO: Test this with 0,0 lines and polygons
        // _floats = new float[_commands.Length << 2];
    }

    public abstract ParsedGeometry Parse(VectorTileFeature feature);

    //public abstract string ToString();
    // {
    //     return DeclaredType switch
    //     {
    //         GeometryType.Point when this is PointGeometry pg => $"Geometry: {pg.Points.Count} points",
    //         GeometryType.Polyline => $"Geometry: ",//{Polylines.Count} polylines",
    //         GeometryType.Polygon => $"Geometry: ",//{Polygons.Count} polygons",
    //         _ => "Geometry: (unknown)"
    //     };
    // }
    
    // TODO: SIMD optimization
    public static void ScaleAll(float[] points, int count, float scale)
    {
        count = Math.Min(points.Length, count);
        for (int i = 0; i < count; i++)
        {
            points[i] = points[i] *= scale;
        }
    }
}