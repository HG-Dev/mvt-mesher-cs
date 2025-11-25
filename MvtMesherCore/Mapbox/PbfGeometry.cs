using System;
using System.Numerics;
using MvtMesherCore.Collections;
using MvtMesherCore.Mapbox.Geometry;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// Represents polyline geometry.
/// </summary>
public readonly struct Polyline(ReadOnlyMemory<Vector2> points)
{
    public readonly ReadOnlyMemory<Vector2> Points = points;
}

/// <summary>
/// Represents polygon geometry. Each polygon consists of one or more rings.
/// </summary>
public readonly struct Polygon(ReadOnlyMemory<ReadOnlyMemory<Vector2>> rings)
{
    public readonly ReadOnlyMemory<ReadOnlyMemory<Vector2>> Rings = rings;
}

/// <summary>
/// Provides methods to parse Mapbox Vector Tile (MVT) geometry commands into higher-level primitives.
/// </summary>
public static class PbfGeometry
{
    enum CommandType { MoveTo = 1, LineTo = 2, ClosePath = 7 }


}