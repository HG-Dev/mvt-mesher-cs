using Mapbox.VectorTile;
using MvtMesherCore;
using MvtMesherCore.Mapbox.Geometry;
using MvtMesherCore.Models;
using VectorTile = MvtMesherCore.Mapbox.VectorTile;

namespace Tests.Protobuf;

[TestFixture]
public class PbfGeometryTests
{
    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile)]
    public void AllUnscaledPointFeaturesShouldBeEquivalent(string pbfFolder, string pbfFile)
    {
        // Arrange
        Assert.That(Path.GetExtension(pbfFile), Is.EqualTo(".pbf"), "This test requires a PBF file path");
        var pbfData = File.ReadAllBytes(Path.Combine(pbfFolder, pbfFile));
        Assert.That(pbfData.Length, Is.GreaterThan(0), "PBF data should not be empty");
        var rawPbfPoints = PbfUtility.FindAllPointFeaturePoints(pbfData);
        Assert.That(rawPbfPoints.Count, Is.GreaterThan(0), "Expected to find at least one point in the PBF data");
        var decodedPbfPoints = rawPbfPoints
            .Select(pt => ((long)pt.X.ZigZagDecode(), (long)pt.Y.ZigZagDecode()));

        // Act
        VectorTile vectorTile = VectorTile.FromByteArray(pbfData, CanonicalTileId.FromDelimitedPatternInString(pbfFile, '-'), Constants.ReadSettings);
        IEnumerable<(ulong, (long, long))> featureIdAndMesherDecodedPoints = vectorTile.Layers.SelectMany(layer => layer.FeatureGroups.EnumerateIndividualFeatures())
            .Where(f => f.GeometryType == GeometryType.Point)
            .Select(f => (f.Id, f.Geometry as PointGeometry))
            .Where(fg => fg.Item2 is not null)
            .Select(fg => (fg.Item1, ((long)fg.Item2!.Points[0].X, (long)fg.Item2!.Points[0].Y)));
        var mesherDecodedPoints = featureIdAndMesherDecodedPoints.Select(t => t.Item2);
        VectorTileReader vectorTileReader = new VectorTileReader(pbfData);
        var mapboxDecodedPoints = new List<(long, long)>();
        foreach (var layerName in vectorTileReader.LayerNames())
        {
            var layer = vectorTileReader.GetLayer(layerName);
            var featureCount = layer.FeatureCount();
            for (int i = 0; i < featureCount; i++)
            {
                var feature = layer.GetFeature(i);
                if (feature.GeometryType != Mapbox.VectorTile.Geometry.GeomType.POINT) continue;
                var geom = feature.Geometry<long>();
                foreach (var row in geom)
                {
                    mapboxDecodedPoints.AddRange(row.Select(pt => (pt.X, pt.Y)));
                }
            }
        }

        // Assert
        Assert.That(mesherDecodedPoints, Is.EquivalentTo(decodedPbfPoints),
            "Decoded points from MvtMesherCore.VectorTile do not match raw decoded points from PBF data");
        Assert.That(mapboxDecodedPoints, Is.EquivalentTo(decodedPbfPoints),
            "Decoded points from Mapbox.VectorTile do not match raw decoded points from PBF data");
    }

    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPolylineBytesFile)]
    public void PolylineGeometryCommandsShouldBeEquivalent(string inFolder, string gcBytesFile)
    {
        var geometryCommands = File.ReadAllBytes(Path.Combine(inFolder, gcBytesFile));
        var geo = new UnparsedGeometry(geometryCommands, GeometryType.Polyline);
        var points = geo.Parse().EnumerateAllPoints().ToList();
        Assert.That(points.Count, Is.GreaterThan(0), "Parsed polygon should have at least one point");
        //TestContext.Out.WriteLine($"Parsed geometry from {Path.GetFileName(gcBytesPath)} via mesher: {string.Join(", ", points.Select(pt => $"({pt.X}, {pt.Y})"))}");

        var commands = PbfSpan.ReadVarintPackedField(geometryCommands).Select(v => v.ToUInt32()).ToList();
        var mapboxGeometry = OriginalMapboxUtility.GetGeometry(2, commands).SelectMany(ring => ring).ToList();
        Assert.That(mapboxGeometry.Count, Is.GreaterThan(0), "Parsed polygon should have at least one point");
        //TestContext.Out.WriteLine($"Parsed geometry from {Path.GetFileName(gcBytesPath)} via Mapbox.VectorTile: {string.Join(", ", mapboxGeometry.Select(pt => $"({pt.X}, {pt.Y})"))}");

        Assert.That(points.Count, Is.EqualTo(mapboxGeometry.Count), "Number of points parsed by MvtMesherCore.VectorTile does not match number parsed by Mapbox.VectorTile");
    }

    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile)]
    public void AllUnscaledPolylineFeaturesShouldBeEquivalent(string inFolder, string pbfFile)
    {
        // Arrange
        Assert.That(Path.GetExtension(pbfFile), Is.EqualTo(".pbf"), "This test requires a PBF file path");
        var pbfData = File.ReadAllBytes(Path.Combine(inFolder, pbfFile));
        Assert.That(pbfData.Length, Is.GreaterThan(0), "PBF data should not be empty");
        var mvtFeatures = PbfUtility.FindAllPolylineFeatures(pbfData);
        Assert.That(mvtFeatures.Count, Is.GreaterThan(0), "Expected to find at least one polyline in the PBF data");

        // Act
        VectorTile vectorTile = VectorTile.FromByteArray(pbfData, CanonicalTileId.FromDelimitedPatternInString(pbfFile, '-'), Constants.ReadSettings);
        var mesherFeatures = vectorTile.Layers.SelectMany(layer => layer.FeatureGroups.EnumerateIndividualFeatures()
            .Where(f => f.GeometryType == GeometryType.Polyline)
            .Select(MvtJsonFeature.FromVectorTileFeature))
            .ToHashSet();

        VectorTileReader vectorTileReader = new VectorTileReader(pbfData);
        var mapboxFeatures = new HashSet<MvtJsonFeature>();
        foreach (var layerName in vectorTileReader.LayerNames())
        {
            var layer = vectorTileReader.GetLayer(layerName);
            var featureCount = layer.FeatureCount();
            for (int i = 0; i < featureCount; i++)
            {
                var feature = layer.GetFeature(i);
                if (feature.GeometryType != Mapbox.VectorTile.Geometry.GeomType.LINESTRING) continue;
                var geom = feature.Geometry<long>();
                var polylinePoints = geom.SelectMany(part => part.Select(pt => (MvtUnscaledJsonPoint)(pt.X, pt.Y))).ToList();
                var mvtFeature = new MvtJsonFeature()
                {
                    Id = feature.Id,
                    ParentLayerName = layerName,
                    GeometryType = 2, // Polyline
                    GeometryPoints = polylinePoints
                };

                mapboxFeatures.Add(mvtFeature);
            }
        }

        // Assert
        Assert.That(mesherFeatures.Count, Is.GreaterThan(0), "Expected to find at least one polyline feature decoded by MvtMesherCore.VectorTile");
        Assert.That(mesherFeatures.Count, Is.EqualTo(mvtFeatures.Count),
            "Number of polyline features decoded by MvtMesherCore.VectorTile does not match number of polylines found in raw PBF data");
        Assert.That(mapboxFeatures.Count, Is.GreaterThan(0), "Expected to find at least one polyline feature decoded by Mapbox.VectorTile");
        Assert.That(mapboxFeatures.Count, Is.EqualTo(mvtFeatures.Count),
            "Number of polyline features decoded by Mapbox.VectorTile does not match number of polylines found in raw PBF data");

        foreach (var mvtFeature in mvtFeatures)
        {
            Assert.That(mesherFeatures, Does.Contain(mvtFeature),
                $"MvtMesherCore.VectorTile is missing polyline feature with Id={mvtFeature.Id} and {mvtFeature.GeometryPoints.Count} points");

            Assert.That(mapboxFeatures, Does.Contain(mvtFeature),
                $"Mapbox.VectorTile is missing polyline feature with Id={mvtFeature.Id} and {mvtFeature.GeometryPoints.Count} points.\n"
                + $"Closest match for {mvtFeature}: {mapboxFeatures.FirstOrDefault(f => f.Id == mvtFeature.Id)?.ToString() ?? "None"}");
        }
    }

    [TestCase(Constants.TestInputFolder, Constants.AtlanticPolygonBytesFile)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPolygonBytesFile)]
    public void PolygonsWithHolesShouldBeParsedCorrectly(string gcBytesFolder, string gcBytesFile)
    {
        var geometryCommands = File.ReadAllBytes(Path.Combine(gcBytesFolder, gcBytesFile));
        var geo = new UnparsedGeometry(geometryCommands, GeometryType.Polygon).Parse() as PolygonGeometry;
        Assert.That(geo, Is.Not.Null, "Parsed geometry should not be null");
        Assert.That(geo!.Polygons.Count, Is.GreaterThan(0), "Parsed polygon feature should have at least one polygon");
        foreach (var polygon in geo.Polygons)
        {
            Assert.That(polygon.AllRings.Count, Is.GreaterThan(0), "Parsed polygon should have at least one ring");
            Assert.That(polygon.ExteriorRing[0], Is.EqualTo(polygon.ExteriorRing[^1]),
                "Exterior ring of polygon should be closed (first and last points should be identical)");
            //TestContext.Out.WriteLine($"Parsed polygon with {polygon.AllRings.Count} rings; exterior ring has {polygon.ExteriorRing.Count} points:\n" +
            //    $"{string.Join(", ", polygon.ExteriorRing.Select(pt => $"({pt.X}, {pt.Y})"))}");
            for (int ringIdx = 0; ringIdx < polygon.InteriorRings.Count; ringIdx++)
            {
                var ring = polygon.InteriorRings[ringIdx];
                Assert.That(ring[0], Is.EqualTo(ring[^1]),
                    $"Interior ring {ringIdx} of polygon should be closed (first and last points should be identical)");
            }
        }
    }

    [TestCase(Constants.TestInputFolder, Constants.AtlanticPolygonBytesFile)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPolygonBytesFile)]
    public void UnscaledPolygonFeatureShouldBeEquivalent(string gcBytesFolder, string gcBytesFile)
    {
        var geometryCommands = File.ReadAllBytes(Path.Combine(gcBytesFolder, gcBytesFile));
        var geo = new UnparsedGeometry(geometryCommands, GeometryType.Polygon).Parse() as PolygonGeometry;
        var polygons = new List<List<List<MvtUnscaledJsonPoint>>>();
        foreach (var poly in geo!.Polygons)
        {
            var rings = new List<List<MvtUnscaledJsonPoint>>();
            foreach (var ring in poly.AllRings)
            {
                var ringPoints = ring.Select(pt => MvtUnscaledJsonPoint.FromVector2(pt)).ToList();
                rings.Add(ringPoints);
            }
            polygons.Add(rings);
        }
        Assert.That(polygons.Count, Is.GreaterThan(0), "Parsed polygon should have at least one polygon");
        Assert.That(polygons.All(p => p.Count > 0), Is.True, "Parsed polygons should have at least one ring each");
        var pointsEnumerated = geo.EnumerateAllPoints().Select(MvtUnscaledJsonPoint.FromVector2).ToList();
        var points = polygons.SelectMany(p => p).SelectMany(r => r).ToList();
        Assert.That(pointsEnumerated, Is.EquivalentTo(points), "EnumerateAllPoints output should match points obtained from rings");
        var polygonRingCounts = string.Join(", ", polygons.Select((p, idx) => $"Pgon {idx}: ({string.Join(", ", p.Select(r => r.Count))})"));
        //TestContext.Out.WriteLine($"Parsed geometry from {Path.GetFileName(gcBytesPath)} via mesher. Resulting polygon ring counts: {polygonRingCounts}");

        var commands = PbfSpan.ReadVarintPackedField(geometryCommands).Select(v => v.ToUInt32()).ToList();
        // Mapbox acquires polygon rings, but does not group them into polygons.
        var mapboxRings = OriginalMapboxUtility.GetGeometry(3, commands);

        var mapboxPoints = mapboxRings.SelectMany(ring => ring).ToList();
        Assert.That(mapboxPoints.Count, Is.GreaterThan(0), "Parsed polygon should have at least one point");
        //TestContext.Out.WriteLine($"Parsed geometry from {Path.GetFileName(gcBytesPath)} via Mapbox.VectorTile: {string.Join(", ", mapboxRings)}");
        Assert.That(points.Count, Is.EqualTo(mapboxPoints.Count), "Number of points parsed by MvtMesherCore.VectorTile does not match number parsed by Mapbox.VectorTile");
        Assert.That(points, Is.EquivalentTo(mapboxPoints),
            "Points parsed by MvtMesherCore.VectorTile do not match points parsed by Mapbox.VectorTile");
    }

    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile)]
    public void AllUnscaledPolygonFeaturesShouldBeEquivalent(string pbfFolder, string pbfFile)
    {
        // Arrange
        Assert.That(Path.GetExtension(pbfFile), Is.EqualTo(".pbf"), "This test requires a PBF file path");
        var pbfData = File.ReadAllBytes(Path.Combine(pbfFolder, pbfFile));
        Assert.That(pbfData.Length, Is.GreaterThan(0), "PBF data should not be empty");
        var mvtFeatures = PbfUtility.FindAllPolygonFeatures(pbfData);
        Assert.That(mvtFeatures.Count, Is.GreaterThan(0), "Expected to find at least one polyline in the PBF data");

        // Act
        VectorTile vectorTile = VectorTile.FromByteArray(pbfData, CanonicalTileId.FromDelimitedPatternInString(pbfFile, '-'), Constants.ReadSettings);
        var mesherFeatures = vectorTile.Layers.SelectMany(layer => layer.FeatureGroups.EnumerateIndividualFeatures()
            .Where(f => f.GeometryType == GeometryType.Polygon)
            .Select(MvtJsonFeature.FromVectorTileFeature))
            .ToHashSet();
        
        VectorTileReader vectorTileReader = new VectorTileReader(pbfData);
        var mapboxFeatures = new HashSet<MvtJsonFeature>();
        foreach (var layerName in vectorTileReader.LayerNames())
        {
            var layer = vectorTileReader.GetLayer(layerName);
            var featureCount = layer.FeatureCount();
            for (int i = 0; i < featureCount; i++)
            {
                var feature = layer.GetFeature(i);
                if (feature.GeometryType != Mapbox.VectorTile.Geometry.GeomType.POLYGON) continue;
                var geom = feature.Geometry<long>();
                var polygonPoints = geom.SelectMany(ring => ring.Select(pt => (MvtUnscaledJsonPoint)(pt.X, pt.Y))).ToList();
                var mvtFeature = new MvtJsonFeature()
                {
                    Id = feature.Id,
                    ParentLayerName = layerName,
                    GeometryType = 3, // Polygon
                    GeometryPoints = polygonPoints
                };
                mapboxFeatures.Add(mvtFeature);
            }
        }

        // Assert
        Assert.That(mesherFeatures.Count, Is.GreaterThan(0), "Expected to find at least one polygon feature decoded by MvtMesherCore.VectorTile");
        Assert.That(mesherFeatures.Count, Is.EqualTo(mvtFeatures.Count),
            "Number of polygon features decoded by MvtMesherCore.VectorTile does not match number of polygons found in raw PBF data");

        foreach (var mvtFeature in mvtFeatures)
        {
            Assert.That(mesherFeatures, Does.Contain(mvtFeature),
                $"MvtMesherCore.VectorTile is missing polygon feature with Id={mvtFeature.Id} and {mvtFeature.GeometryPoints.Count} points");
            Assert.That(mapboxFeatures, Does.Contain(mvtFeature),
                $"Mapbox.VectorTile is missing polygon feature with Id={mvtFeature.Id} and {mvtFeature.GeometryPoints.Count} points");
            //TestContext.Out.WriteLine("Verified polygon feature Id={0} with {1} points", mvtFeature.Id, mvtFeature.GeometryPoints.Count);
        }
    }
}