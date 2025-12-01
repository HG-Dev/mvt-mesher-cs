using System;
using System.Collections.Generic;
using System.Numerics;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Base class for geometries parsed from MVT data.
/// </summary>
/// <remarks>
/// TODO: 1. Duck typing: look at slice counts to figure out what the type is
/// </remarks>
public abstract class BaseGeometry
{
    /// <summary>
    /// The declared geometry type of this geometry.
    /// </summary>
    public readonly GeometryType DeclaredType;
    /// <summary>
    /// Indicates whether this geometry has been parsed.
    /// </summary>
    public abstract bool Parsed { get; }
    /// <summary>
    /// The number of major elements (e.g., points for point geometry, polylines for polyline geometry, polygons for polygon geometry).
    /// </summary>
    public virtual int MajorElementCount => 0;

    /// <summary>
    /// Enumerates all points in this geometry.
    /// </summary>
    public virtual IEnumerable<Vector2> EnumerateAllPoints()
    {
        yield break;
    }

    /// <summary>
    /// Creates a new Geometry with the specified declared type.
    /// </summary>
    /// <param name="declaredType">Type of the geometry.</param>
    public BaseGeometry(GeometryType declaredType)
    {
        DeclaredType = declaredType;
    }

    /// <summary>
    /// Parses this geometry into a ParsedGeometry instance, or returns this if already parsed.
    /// </summary>
    /// <param name="scale">Scalar applied to the geometry during parsing.</param>
    public abstract ParsedGeometry Parse(float scale = 1f);
    
    /// <summary>
    /// Scales all float values in the given array by the specified scale factor.
    /// </summary>
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