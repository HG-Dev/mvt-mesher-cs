using System;

namespace MvtMesherCore.Mapbox.Geometry;

public class UnparsedGeometry(ReadOnlyMemory<byte> commands, GeometryType type) : BaseGeometry(type)
{
    public override bool Parsed => false;
    
    public override ParsedGeometry Parse(VectorTileFeature feature)
    {
        try
        {
            return DeclaredType switch
            {
                GeometryType.Point => PointGeometry.CreateFromCommands(commands.Span, (float)1 / feature.ParentLayer.Extent),
                GeometryType.Polyline => PolylineGeometry.CreateFromCommands(commands.Span, (float)1 / feature.ParentLayer.Extent),
                GeometryType.Polygon => PolygonGeometry.CreateFromCommands(commands.Span, (float)1 / feature.ParentLayer.Extent),
                _ => throw new PbfReadFailure($"{DeclaredType} is unsupported geometry type"),
            };
        }
        catch (PbfReadFailure geometryIssue)
        {
            throw new PbfValidationFailure(PbfValidation.Geometry, $"{DeclaredType} read failure on {feature}", geometryIssue);
        }
        catch (ArgumentOutOfRangeException alloationIssue)
        {
            throw new PbfValidationFailure(PbfValidation.Geometry, $"{DeclaredType} read failure on {feature}", alloationIssue);
        }
    }
}