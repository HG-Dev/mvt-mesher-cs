using Mapbox.VectorTile;
using MvtMesherCore.Models;
using Newtonsoft.Json.Linq;

namespace Tests.Protobuf;

[TestFixture]
public class PbfOriginalMapboxTests
{
    [TestCase(Constants.EnoshimaPbfPath, Constants.EnoshimaJsonPath)]
    [TestCase(Constants.AtlanticPbfPath, Constants.AtlanticJsonPath)]
    public void ExportMvtJsonUsingOriginalMapboxMethods(string pbfPath, string outfileJson)
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

                featureObjs.Add(new MvtJsonFeature()
                {
                    Id = feature.Id,
                    GeometryType = (byte)feature.GeometryType,
                    Properties = propertyDict
                });

                if (feature.Layer.Keys.Contains("name") && feature.GetValue("name") is string name)
                {
                    TestContext.Out.WriteLine($"Feature {feature.Id} in layer {layerName} has name: {name}");
                }
                else
                {
                    TestContext.Out.WriteLine($"Feature {feature.Id} in layer {layerName} has no name");
                }
            }
            layerObj.Features = featureObjs;
            mvtJson.Layers[layerName] = layerObj;
        }
    
        using var jsonFile = File.Open(outfileJson, FileMode.Create, FileAccess.Write);
        using var jsonWriter = new StreamWriter(jsonFile);
        Newtonsoft.Json.JsonSerializer serializer = new()
        {
            Formatting = Newtonsoft.Json.Formatting.Indented
        };
        serializer.Serialize(jsonWriter, mvtJson);
    }
}