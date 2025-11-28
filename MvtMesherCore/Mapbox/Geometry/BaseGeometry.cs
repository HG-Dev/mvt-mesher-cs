using System;
using System.Collections.Generic;
using System.Numerics;

namespace MvtMesherCore.Mapbox.Geometry;

// TODO: Optimize this by converting its underlying data into a 1D array of float and return things as slices of array
// Duck typing: look at slice counts to figure out what the type is
public abstract class BaseGeometry
{
    public readonly GeometryType DeclaredType;
    public abstract bool Parsed { get; }
    public virtual int MajorElementCount => 0;

    public virtual IEnumerable<Vector2> EnumerateAllPoints()
    {
        yield break;
    }

    public BaseGeometry(GeometryType declaredType)
    {
        DeclaredType = declaredType;
    }

    public abstract ParsedGeometry Parse(float scale = 1f);
    
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