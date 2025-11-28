using System;

namespace MvtMesherCore.Mapbox.Geometry;

public class UnparsedGeometry(ReadOnlyMemory<byte> commands, GeometryType type) : BaseGeometry(type)
{
    public override bool Parsed => false;
    
    public override ParsedGeometry Parse(float scale = 1f)
    {
        return DeclaredType switch
        {
            GeometryType.Point => PointGeometry.CreateFromCommands(commands.Span, scale),
            GeometryType.Polyline => PolylineGeometry.CreateFromCommands(commands.Span, scale),
            GeometryType.Polygon => PolygonGeometry.CreateFromCommands(commands.Span, scale),
            _ => throw new PbfReadFailure($"{DeclaredType} is unsupported geometry type"),
        };
    }
}