using MvtMesherCore;
using MvtMesherCore.Mapbox;
using MvtMesherCore.Models;
using VectorTile = MvtMesherCore.Mapbox.VectorTile;

namespace Tests.Protobuf;

[TestFixture]
public class PbfTests
{
    //[Ignore("Comparison test using MvtMesherCore parsing against expected JSON")]
    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile, Constants.AtlanticJsonFile)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile, Constants.EnoshimaJsonFile)]
    public void PbfFileShouldParseWithEquivalenceToJson(string inFolder, string pbfPath, string jsonPath)
    {
        using var stream = File.OpenRead(Path.Combine(inFolder, pbfPath));
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'), Constants.ReadSettingsStrict);
        
        var expectedJson = Path.Combine(inFolder, jsonPath).LoadAsMvtJson();
        JsonUtility.AssertEquivalency(vectorTile, expectedJson);
    }

    [TestCase(Constants.TestInputFolder, Constants.ToranomonPbfFile)]
    public void BigPbfFileShouldParseWithoutErrors(string inFolder, string pbfPath)
    {
        var bigPbfPath = Path.Combine(inFolder, pbfPath);
        using var stream = File.OpenRead(bigPbfPath);
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString("0-0-0", '-'), Constants.ReadSettingsStandard);
        
        Assert.That(vectorTile.Layers.Count, Is.GreaterThan(0));

        foreach (var layer in vectorTile.Layers)
        {
            Assert.That(layer.FeatureGroups.Count, Is.GreaterThan(0), $"Layer '{layer.Name}' should have features.");
            foreach (var feature in layer.FeatureGroups.EnumerateIndividualFeatures())
            {
                Assert.DoesNotThrow(() => {
                    var geom = feature.Geometry;
                }, $"Feature ID {feature.Id} in layer '{layer.Name}' should parse geometry without errors.");
            }
        }
    }
}