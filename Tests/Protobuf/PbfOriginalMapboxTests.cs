using Mapbox.VectorTile;
using Mapbox.VectorTile.Geometry;
using MvtMesherCore.Models;
using GeometryType = MvtMesherCore.Mapbox.Geometry.GeometryType;

namespace Tests.Protobuf;

[Ignore("Utility tests for extracting geometry commands and exporting MvtJson using original Mapbox methods")]
[TestFixture]
public class PbfOriginalMapboxTests
{
    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile, (ulong)75124043, GeometryType.Polygon, Constants.TestOutputFolder)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile, (ulong)1916566182, GeometryType.Polyline, Constants.TestOutputFolder)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile, (ulong)1915597462, GeometryType.Polygon, Constants.TestOutputFolder)]
    public void ExtractAnomalousGeometryCommands(string inFolder, string pbfFile, ulong id, GeometryType geoType, string outFolder)
    {
        using var stream = File.OpenRead(Path.Combine(inFolder, pbfFile));
        using var byteReader = new BinaryReader(stream);
        if (!PbfUtility.TryFindGeometryCommandBytesForFeature(byteReader.ReadBytes((int)stream.Length),
                featureId: id,
                geometryType: geoType,
                out ReadOnlyMemory<byte> geometryCommands))
        {
            throw new Exception("Failed to find geometry commands for feature");
        }

        var geo = new MvtMesherCore.Mapbox.Geometry.UnparsedGeometry(geometryCommands, geoType);
        Assert.DoesNotThrow(() =>
        {
            var parsed = geo.Parse();
            TestContext.Out.WriteLine($"Parsed geometry: {string.Join(", ", parsed.EnumerateAllPoints().Select(pt => $"({pt.X}, {pt.Y})"))}");
        });

        var tileId = CanonicalTileId.FromDelimitedPatternInString(pbfFile, '-').ToShortString('-');
        using var bytesFile = File.Open(Path.Combine(outFolder, $"{tileId}_f{id}-{(byte)geoType}_gc.bytes"), FileMode.Create, FileAccess.Write);
        using var bytesWriter = new BinaryWriter(bytesFile);
        bytesWriter.Write(geometryCommands.Span);
    }

    [TestCase(Constants.EnoshimaPbfFile, Constants.EnoshimaJsonFile, Constants.TestOutputFolder)]
    [TestCase(Constants.AtlanticPbfFile, Constants.AtlanticJsonFile, Constants.TestOutputFolder)]
    public void ExportMvtJsonUsingOriginalMapboxMethods(string pbfPath, string outfileJson, string outFolder)
    {
        using var stream = File.OpenRead(pbfPath);
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTileReader = new VectorTileReader(bytes, validate: false);

        MvtJson mvtJson = new MvtJson()
        {
            Layers = new Dictionary<string, MvtJsonLayer>(),
            TileId = CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-').ToShortString()
        };

        foreach (var layerName in vectorTileReader.LayerNames())
        {
            var layer = vectorTileReader.GetLayer(layerName);
            var layerObj = new MvtJsonLayer()
            {
                PropertyNames = new List<string>(layer.Keys),
                PropertyValues = new List<string>(layer.Values.Select(v => v?.ToString() ?? throw new Exception("Null property value"))),
                Version = layer.Version,
                Extent = layer.Extent
            };
            var featureObjs = new List<MvtJsonFeature>();
            var featureCount = layer.FeatureCount();
            for (int i = 0; i < featureCount; i++)
            {
                var feature = layer.GetFeature(i);
                var propertyDict = new Dictionary<string, string>();
                
                for (int tagIdx = 1; tagIdx < feature.Tags.Count; tagIdx += 2)
                {
                    var key = layer.Keys[feature.Tags[tagIdx - 1]];
                    var value = layer.Values[feature.Tags[tagIdx]];
                    propertyDict[key] = value?.ToString() ?? throw new Exception("Null property value");
                }

                List<MvtUnscaledJsonPoint> parsedGeometry = DecodeGeometry.GetGeometry(
                    (ulong)layer.Extent, // Extent isn't used
                    feature.GeometryType,
                    feature.GeometryCommands,
                    scale: 1.0f // Neither is scale. Go figure.
                ).SelectMany(part => part.Select(pt => (MvtUnscaledJsonPoint)(pt.X, pt.Y))).ToList();
                featureObjs.Add(new MvtJsonFeature()
                {
                    Id = feature.Id,
                    GeometryType = (byte)feature.GeometryType,
                    GeometryPoints = parsedGeometry,
                    Properties = propertyDict
                });
            }
            layerObj.Features = featureObjs;
            mvtJson.Layers[layerName] = layerObj;
        }
    
        using var jsonFile = File.Open(Path.Combine(outFolder, outfileJson), FileMode.Create, FileAccess.Write);
        using var jsonWriter = new StreamWriter(jsonFile);
        Newtonsoft.Json.JsonSerializer serializer = new()
        {
            Formatting = Newtonsoft.Json.Formatting.Indented
        };
        serializer.Serialize(jsonWriter, mvtJson);
    }
}