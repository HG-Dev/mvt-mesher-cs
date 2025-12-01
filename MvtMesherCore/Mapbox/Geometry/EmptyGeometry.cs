using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Geometry that was parsed from MVT data, but contains no points.
/// </summary>
public class EmptyGeometry(GeometryType declaredType) : ParsedGeometry(declaredType)
{
    /// <inheritdoc/>
    public override string ToString() => string.Concat(nameof(EmptyGeometry), "()");
}