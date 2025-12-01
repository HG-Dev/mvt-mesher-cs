namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Types of geometries in MVT data.
/// </summary>
public enum GeometryType : byte
{
    /// <summary>
    /// Unknown geometry type.
    /// </summary>
    Unknown,
    /// <summary>
    /// Point geometry type: single point(s).
    /// </summary>
    Point,
    /// <summary>
    /// Polyline geometry type: one or more polylines.
    /// </summary>
    Polyline,
    /// <summary>
    /// Polygon geometry type: one or more polygons.
    /// </summary>
    Polygon
}