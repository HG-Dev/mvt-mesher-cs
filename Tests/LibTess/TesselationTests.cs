using LibTessDotNet;
using MvtMesherCore.Mapbox;
using MvtMesherCore.Mapbox.Geometry;
using MvtMesherCore.LibTessDotNetIntegration;
using MvtMesherCore.Models;

namespace Tests.LibTess;

[TestFixture]
public class TesselationTests
{
    public const string EnoshimaPbfPath = "Res/14-14540-6473_enoshima.pbf";

    /// <summary>
    /// Polygon geometry obtained from a PBF file should be tesselation-ready.
    /// </summary>
    [TestCase(EnoshimaPbfPath, 7)]
    public void PbfGeometryFeaturesShouldCreateMeshes(string pbfPath, int expectedPolygonCount)
    {
        using var stream = File.OpenRead(pbfPath);
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'));
        var polygonGeometries = vectorTile.LayersByName.SelectMany(layerPair => layerPair.Value.FeatureGroups.EnumerateIndividualFeatures())
            .Where(feature => feature.Geometry is PolygonGeometry)
            .Select(feature => (feature, (PolygonGeometry)feature.Geometry))
            .ToList();
        TestContext.Out.WriteLine($"Found {polygonGeometries.Count} polygon-bearing features");
        Assert.That(polygonGeometries.Count, Is.EqualTo(expectedPolygonCount));
        
        foreach (var (feature, geometry) in polygonGeometries)
        {
            foreach (var polygon in geometry.Polygons)
            {
                var tess = new LibTessDotNet.Tess();
                var originalPointCount = polygon.AllRings.Sum(ring => ring.Count);
                var contours = polygon.ToContours();
                foreach (var contour in contours)
                {
                    tess.AddContour(contour);
                }
                tess.Tessellate();

                //TestContext.Out.WriteLine($"Polygon from {feature.ParentLayer.Name}/{feature.Name} with {polygon.AllRings.Count} rings and {originalPointCount} points produced {tess.ElementCount} triangles and {tess.Vertices.Length} vertices");
            }
        }

    }
}