using Mapbox.VectorTile;
using MvtMesherCore;
using MvtMesherCore.Mapbox;
using MvtMesherCore.Models;
using VectorTile = MvtMesherCore.Mapbox.VectorTile;

namespace Tests.Protobuf;

[TestFixture]
public class PbfTests
{
    public const string EnoshimaPbfPath = "Res/14-14540-6473_enoshima.pbf";
    public const string EnoshimaExpectedLayers = "boundary,building,landcover,landuse,mountain_peak,place," +
                                                 "poi,transportation,transportation_name,water,water_name";

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
    
    [TestCase(EnoshimaPbfPath, EnoshimaExpectedLayers)]
    public void ValidPbfHasExpectedLayerNamesOriginalMapbox(string pbfPath, string layerNameCsv)
    {
        using var stream = File.OpenRead(pbfPath);
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTileReader = new VectorTileReader(bytes, validate: false);
        Assert.That(vectorTileReader.LayerNames(), Is.EquivalentTo(layerNameCsv.Split(',')));
        foreach (var layerName in vectorTileReader.LayerNames())
        {
            var layer = vectorTileReader.GetLayer(layerName);
            var featureCount = layer.FeatureCount();
            TestContext.Out.WriteLine($"{layerName} (v{layer.Version}) has {featureCount} features; {layer.Keys.Count} keys and {layer.Values.Count} values");
            for (int i = 0; i < featureCount; i++)
            {
                var feature = layer.GetFeature(i);
                TestContext.Out.WriteLine($"{feature.Id} {feature.GeometryType}: {feature.GeometryCommands.Count} commands; {feature.Tags.Count} tags");
                TestContext.Out.WriteLine($"Tags = {string.Join(", ", feature.Tags)}");
            }
        }
    }
    
    [TestCase(EnoshimaPbfPath, EnoshimaExpectedLayers)]
    public void ValidPbfHasExpectedLayerNames(string pbfPath, string layerNameCsv)
    {
        using var stream = File.OpenRead(pbfPath);
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'));
        Assert.That(vectorTile.LayerNames, Is.EquivalentTo(layerNameCsv.Split(',')));
        foreach (var (layerName, layer) in vectorTile)
        {
            var featureCount = layer.Features.Count;
            TestContext.Out.WriteLine($"{layerName} (v{layer.Version}) has {featureCount} features, {layer.PropertyNames.Count} keys and {layer.PropertyValues.Count} values");
            foreach (var (id, feature) in layer.Features)
            {
                Assert.That(feature.Id, Is.EqualTo(id));
                TestContext.Out.WriteLine($"{id} {feature.Geometry}; {feature.Properties.Count} key-value pairs");
            }
        }
    }
}