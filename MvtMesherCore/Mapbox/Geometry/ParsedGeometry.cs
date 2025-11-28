using System;
using System.Runtime.CompilerServices;

namespace MvtMesherCore.Mapbox.Geometry;

public abstract class ParsedGeometry(GeometryType declaredType) : BaseGeometry(declaredType)
{
    public override bool Parsed => true;
    public override ParsedGeometry Parse(float scale = 1f) => this;
}