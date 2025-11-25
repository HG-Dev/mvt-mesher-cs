using MvtMesherCore.Collections;

namespace MvtMesherCore.Mapbox.Geometry;

public class EmptyGeometry(GeometryType declaredType) : ParsedGeometry(declaredType)
{
    public override string ToString() => string.Concat(nameof(EmptyGeometry), "()");
}