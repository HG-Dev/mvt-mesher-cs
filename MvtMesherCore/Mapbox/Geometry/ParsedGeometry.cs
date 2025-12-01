using System;
using System.Runtime.CompilerServices;

namespace MvtMesherCore.Mapbox.Geometry;

/// <summary>
/// Base class for geometries parsed from MVT data.
/// </summary>
public abstract class ParsedGeometry(GeometryType declaredType) : BaseGeometry(declaredType)
{
    /// <inheritdoc/>
    public override bool Parsed => true;
    /// <inheritdoc/>
    public override ParsedGeometry Parse(float scale = 1f) => this;
}