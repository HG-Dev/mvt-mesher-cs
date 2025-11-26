using MvtMesherCore;
using MvtMesherCore.Mapbox;
using MvtMesherCore.Models;
using VectorTile = MvtMesherCore.Mapbox.VectorTile;

namespace Tests.Protobuf;

[TestFixture]
public class PbfTests
{
    [SetUp]
    public void Setup()
    {
        VectorTile.ValidationLevel = PbfValidation.All & ~PbfValidation.FeatureVersion; // OpenFreeMap features lack version number
        PbfSpan.ExtraTagValidation = tag => tag is {FieldNumber: >= 19000 and <= 19999}
            ? new PbfValidationFailure(PbfValidation.Tags, $"Tag value {tag.FieldNumber} is Mapbox reserved")
            : null;
    }
    
    [TearDown]
    public void TearDown() => VectorTile.ValidationLevel = PbfValidation.None;


    [TestCase(Constants.AtlanticPbfPath, Constants.AtlanticJsonPath)]
    [TestCase(Constants.EnoshimaPbfPath, Constants.EnoshimaJsonPath)]
    public void PbfFileShouldParseWithEquivalenceToJson(string pbfPath, string jsonPath)
    {
        using var stream = File.OpenRead(pbfPath);
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'));
        
        var expectedJson = jsonPath.LoadAsMvtJson();
        JsonUtility.AssertEquivalency(vectorTile, expectedJson);
    }

    //[TestCase(EnoshimaPbfPath, EnoshimaFeatureNameCsvPath)]
    //[TestCase(AtlanticPbfPath, AtlanticFeatureNameCsvPath)]
    // public void ValidPbfHasExpectedFeatureNames(string pbfPath, string jsonPath)
    // {
    //     Dictionary<(string layerName, ulong featureId), string> expectedNames = new();
    //     using (var reader = new StreamReader(jsonPath))
    //     {
    //         var jsonText = reader.ReadToEnd();
    //         var jsonObj = JObject.Parse(jsonText);
    //         var layersObj = (JObject)jsonObj["Layers"]!;
    //         foreach (var layerProp in layersObj.Properties())
    //         {
    //             var layerName = layerProp.Name;
    //             var layerObj = (JObject)layerProp.Value;
    //             var featuresArray = (JArray)layerObj["Features"]!;
    //             foreach (var featureToken in featuresArray)
    //             {
    //                 var featureObj = (JObject)featureToken;
    //                 var featureId = featureObj["Id"]!.Value<ulong>();
    //                 var propertiesObj = (JObject)featureObj["Properties"]!;
    //                 if (propertiesObj.TryGetValue("name", out var nameToken))
    //                 {
    //                     var name = nameToken.Value<string>()!;
    //                     expectedNames.Add((layerName, featureId), name);
    //                 }
    //             }
    //         }
    //     }


    //     using var stream = File.OpenRead(pbfPath);
    //     using var byteReader = new BinaryReader(stream);
    //     var numBytes = stream.Length;
    //     Assert.That(numBytes, Is.LessThan(int.MaxValue));
    //     var bytes = byteReader.ReadBytes((int)numBytes);
    //     var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'));

    //     foreach (var (layerName, layer) in vectorTile)
    //     {
    //         var layerShouldHaveNames = expectedNames.Keys.Any(k => k.layerName == layerName);
    //         var layerActuallyHasNames = layer.PropertyNames.Contains(MvtMesherCore.Mapbox.VectorTileFeature.NAME_PROPERTY_KEY);
    //         Assert.That(layerActuallyHasNames, Is.EqualTo(layerShouldHaveNames), $"Layer {layerName} name property presence mismatch");
    //         TestContext.Out.WriteLine($"Layer properties: {string.Join(", ", layer.PropertyNames)}");
    //         TestContext.Out.WriteLine($"Layer values: {string.Join(", ", layer.PropertyValues)}");
    //         foreach (var (id, feature) in layer.Features)
    //         {
    //             if (expectedNames.TryGetValue((layerName, id), out var expectedName))
    //             {
    //                 var actualName = feature.Name;
    //                 Assert.That(actualName, Is.EqualTo(expectedName), $"Feature {id} in layer {layerName} name mismatch");
    //             }
    //             else if (feature.Properties.ContainsKey(MvtMesherCore.Mapbox.VectorTileFeature.NAME_PROPERTY_KEY))
    //             {
    //                 Assert.Fail($"Feature {id} in layer {layerName} has unexpected name property: {feature.Name}");
    //             }
    //         }
    //     }
    // }
}