using MvtMesherCore;
using MvtMesherCore.Mapbox.Geometry;
using MvtMesherCore.Models;

public static class PbfUtility
{
    public static List<(Varint X, Varint Y)> FindAllPointFeaturePoints(ReadOnlyMemory<byte> pbfData)
    {
        var points = new List<(Varint X, Varint Y)>();
        var layersChecked = 0;
        var featuresChecked = 0;

        foreach (var layerMemory in PbfMemory.FindAndEnumerateFieldsWithTag(pbfData,
                        MvtMesherCore.Mapbox.VectorTile.PbfTags.Layers))
        {
            layersChecked++;
            foreach (var featureMemory in PbfMemory.FindAndEnumerateFieldsWithTag(layerMemory,
                MvtMesherCore.Mapbox.VectorTileLayer.PbfTags.Features))
            {
                featuresChecked++;
                var id = 0;
                if (PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Id, out int idOffset))
                {
                    id = PbfSpan.ReadVarint(featureMemory.Span, ref idOffset).ToInt32();
                }

                if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Type, out int typeOffset))
                {
                    throw new PbfReadFailure($"Feature {id} missing geometry type tag");
                }
                var geometryType = (GeometryType)PbfSpan.ReadVarint(featureMemory.Span, ref typeOffset).ToUInt64();
                if (geometryType != GeometryType.Point) continue;

                if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Geometry, out int geomOffset))
                {
                    throw new PbfReadFailure($"Feature {id} missing geometry tag");
                }
                var geometryMemory = PbfMemory.ReadLengthDelimited(featureMemory, ref geomOffset);
                var offset = 0;
                while (offset < geometryMemory.Span.Length)
                {
                    var cmdInteger = PbfSpan.ReadVarint(geometryMemory.Span, ref offset).ToUInt32();
                    CanvasCommand commandId = (CanvasCommand)(cmdInteger & 0x07);
                    var pointsToConsume = (int)(cmdInteger >> 3);

                    if (commandId is not CanvasCommand.MoveTo)
                    {
                        throw new PbfReadFailure(
                            $"Encountered unexpected geometry command command {commandId} when parsing {GeometryType.Point}(s) in feature {id}");
                    }
                    
                    for (int i = 0; i < pointsToConsume; i++)
                    {
                        (Varint X, Varint Y) point = new (
                            PbfSpan.ReadVarint(geometryMemory.Span, ref offset),
                            PbfSpan.ReadVarint(geometryMemory.Span, ref offset));
                        points.Add(point);
                    }
                }
            }
        }

        Assert.That(layersChecked, Is.GreaterThan(0), "Expected to find at least one layer in the tile");
        Assert.That(featuresChecked, Is.GreaterThan(0), "Expected to find at least one feature in all layers");
        TestContext.Out.WriteLine($"{nameof(FindAllPointFeaturePoints)} -- Checked {layersChecked} layers and {featuresChecked} features; found {points.Count} raw points");
        return points;
    }

    public static HashSet<MvtJsonFeature> FindAllPolylineFeatures(ReadOnlyMemory<byte> pbfData)
    {
        var features = new HashSet<MvtJsonFeature>();
        var layersChecked = 0;
        var featuresChecked = 0;
        foreach (var layerMemory in PbfMemory.FindAndEnumerateFieldsWithTag(pbfData,
                        MvtMesherCore.Mapbox.VectorTile.PbfTags.Layers))
        {
            layersChecked++;
            string layerName = $"Layer {layersChecked}";
            if (PbfSpan.TryFindFirstTag(layerMemory.Span, MvtMesherCore.Mapbox.VectorTileLayer.PbfTags.Name, out int nameOffset))
            {
                layerName = PbfSpan.ReadString(layerMemory.Span, ref nameOffset);
            }

            foreach (var featureMemory in PbfMemory.FindAndEnumerateFieldsWithTag(layerMemory,
                MvtMesherCore.Mapbox.VectorTileLayer.PbfTags.Features))
            {
                featuresChecked++;
                ulong id = 0;
                if (PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Id, out int idOffset))
                {
                    id = PbfSpan.ReadVarint(featureMemory.Span, ref idOffset).ToUInt64();
                }

                if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Type, out int typeOffset))
                {
                    throw new PbfReadFailure($"Feature {id} missing geometry type tag");
                }
                var geometryType = (GeometryType)PbfSpan.ReadVarint(featureMemory.Span, ref typeOffset).ToUInt64();
                if (geometryType != GeometryType.Polyline) continue;

                if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Geometry, out int geomOffset))
                {
                    throw new PbfReadFailure($"Feature {id} missing geometry tag");
                }
                var geometryMemory = PbfMemory.ReadLengthDelimited(featureMemory, ref geomOffset);
                PolylineGeometry polylineGeometry = new UnparsedGeometry(geometryMemory, geometryType).Parse(1f) as PolylineGeometry 
                    ?? throw new PbfReadFailure($"Failed to parse polyline geometry for feature {id}");

                var feature = new MvtJsonFeature
                {
                    Id = id,
                    ParentLayerName = layerName,
                    GeometryType = 2, // Polyline
                    GeometryPoints = polylineGeometry.EnumerateAllPoints()
                        .Select(pt => new MvtUnscaledJsonPoint { X = (long)pt.X, Y = (long)pt.Y })
                        .ToList()
                };
                var conflict = !features.Add(feature);
                if (conflict)
                {
                    throw new PbfReadFailure($"Duplicate polyline feature {feature} found in tile");
                }
            }
        }
        Assert.That(layersChecked, Is.GreaterThan(0), "Expected to find at least one layer in the tile");
        Assert.That(featuresChecked, Is.GreaterThan(0), "Expected to find at least one feature in all layers");
        TestContext.Out.WriteLine($"{nameof(FindAllPolylineFeatures)} -- Checked {layersChecked} layers and {featuresChecked} features; found {features.Count} polyline features");
        return features;
    }

    public static bool TryFindGeometryCommandBytesForFeature(ReadOnlyMemory<byte> fullPbfData, ulong featureId, GeometryType geometryType, out ReadOnlyMemory<byte> geometryCommands)
    {
        geometryCommands = ReadOnlyMemory<byte>.Empty;
        foreach (var layerMemory in PbfMemory.FindAndEnumerateFieldsWithTag(fullPbfData,
            MvtMesherCore.Mapbox.VectorTile.PbfTags.Layers))
        {
            foreach (var featureMemory in PbfMemory.FindAndEnumerateFieldsWithTag(layerMemory,
                MvtMesherCore.Mapbox.VectorTileLayer.PbfTags.Features))
            {
                ulong id = 0;
                if (PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Id, out int idOffset))
                {
                    id = PbfSpan.ReadVarint(featureMemory.Span, ref idOffset).ToUInt64();
                }

                if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Type, out int typeOffset))
                {
                    throw new PbfReadFailure($"Feature {id} missing geometry type tag");
                }

                var foundGeometryType = (GeometryType)PbfSpan.ReadVarint(featureMemory.Span, ref typeOffset).ToUInt64();

                if (id == featureId && foundGeometryType == geometryType)
                {
                    if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Geometry, out int geomOffset))
                    {
                        throw new PbfReadFailure($"Feature {id} missing geometry tag");
                    }
                    geometryCommands = PbfMemory.ReadLengthDelimited(featureMemory, ref geomOffset);
                    return true;
                }
            }
        }
        return false;
    }

    public static HashSet<MvtJsonFeature> FindAllPolygonFeatures(ReadOnlyMemory<byte> pbfData, bool closeRings = true)
    {
        var features = new HashSet<MvtJsonFeature>();
        var layersChecked = 0;
        var featuresChecked = 0;
        foreach (var layerMemory in PbfMemory.FindAndEnumerateFieldsWithTag(pbfData,
                        MvtMesherCore.Mapbox.VectorTile.PbfTags.Layers))
        {
            layersChecked++;
            string layerName = $"Layer {layersChecked}";
            if (PbfSpan.TryFindFirstTag(layerMemory.Span, MvtMesherCore.Mapbox.VectorTileLayer.PbfTags.Name, out int nameOffset))
            {
                layerName = PbfSpan.ReadString(layerMemory.Span, ref nameOffset);
            }

            foreach (var featureMemory in PbfMemory.FindAndEnumerateFieldsWithTag(layerMemory,
                MvtMesherCore.Mapbox.VectorTileLayer.PbfTags.Features))
            {
                featuresChecked++;
                ulong id = 0;
                if (PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Id, out int idOffset))
                {
                    id = PbfSpan.ReadVarint(featureMemory.Span, ref idOffset).ToUInt64();
                }

                if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Type, out int typeOffset))
                {
                    throw new PbfReadFailure($"Feature {id} missing geometry type tag");
                }
                var geometryType = (GeometryType)PbfSpan.ReadVarint(featureMemory.Span, ref typeOffset).ToUInt64();
                if (geometryType != GeometryType.Polygon) continue;

                if (!PbfSpan.TryFindFirstTag(featureMemory.Span, MvtMesherCore.Mapbox.VectorTileFeature.PbfTags.Geometry, out int geomOffset))
                {
                    throw new PbfReadFailure($"Feature {id} missing geometry tag");
                }
                var geometryMemory = PbfMemory.ReadLengthDelimited(featureMemory, ref geomOffset);
                PolygonGeometry polygonGeometry = new UnparsedGeometry(geometryMemory, geometryType).Parse() as PolygonGeometry 
                    ?? throw new PbfReadFailure($"Failed to parse polygon geometry for feature {id}");

                var feature = new MvtJsonFeature
                {
                    Id = id,
                    ParentLayerName = layerName,
                    GeometryType = 3, // Polygon
                    GeometryPoints = polygonGeometry.EnumerateAllPoints()
                        .Select(pt => new MvtUnscaledJsonPoint { X = (long)pt.X, Y = (long)pt.Y })
                        .ToList()
                };
                var conflict = !features.Add(feature);
                if (conflict)
                {
                    throw new PbfReadFailure($"Duplicate polygon feature {feature} found in tile");
                }
            }
        }

        return features;
    }
}