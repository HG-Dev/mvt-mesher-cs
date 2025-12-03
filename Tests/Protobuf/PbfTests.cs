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

    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile)]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile)]
    [TestCase(Constants.TestInputFolder, Constants.ToranomonPbfFile)]
    public void VectorTileFeature_PropertiesShouldMaskLayerKeys(string inFolder, string pbfPath)
    {
        using var stream = File.OpenRead(Path.Combine(inFolder, pbfPath));
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'), Constants.ReadSettingsStrict);

        foreach (var layer in vectorTile.Layers)
        {
            var layerKeys = layer.PropertyNames;
            var layerValues = layer.PropertyValues;

            foreach (var feature in layer.FeatureGroups.EnumerateIndividualFeatures())
            {
                var properties = feature.Properties;
                var featurePropertyKeys = properties.Keys.ToHashSet();
                foreach (var layerKey in layerKeys)
                {
                    if (featurePropertyKeys.Contains(layerKey))
                    {
                        Assert.DoesNotThrow(() => {
                            var _ = properties[layerKey];
                        }, $"Feature ID {feature.Id} in layer '{layer.Name}' should access property '{layerKey}' without errors.");
                        Assert.That(properties.TryGetValue(layerKey, out var _), Is.True, 
                            $"Feature ID {feature.Id} in layer '{layer.Name}' should have property '{layerKey}'.");
                    }
                    else
                    {
                        Assert.Throws<KeyNotFoundException>(() => {
                            var _ = properties[layerKey];
                        }, $"Feature ID {feature.Id} in layer '{layer.Name}' should not have property '{layerKey}'.");
                        Assert.That(properties.TryGetValue(layerKey, out var _), Is.False, 
                            $"Feature ID {feature.Id} in layer '{layer.Name}' should not have property '{layerKey}'.");
                    }
                }
            }
        }
    }

    [TestCase(Constants.TestInputFolder, Constants.AtlanticPbfFile, "name:fr")]
    [TestCase(Constants.TestInputFolder, Constants.EnoshimaPbfFile, "subclass")]
    [TestCase(Constants.TestInputFolder, Constants.ToranomonPbfFile, "colour")]
    public void VectorTileFeature_PropertiesCopiedToListShouldMaskLayerKeys(string inFolder, string pbfPath, string stringPropToCheck)
    {
        using var stream = File.OpenRead(Path.Combine(inFolder, pbfPath));
        using var byteReader = new BinaryReader(stream);
        var numBytes = stream.Length;
        Assert.That(numBytes, Is.LessThan(int.MaxValue));
        var bytes = byteReader.ReadBytes((int)numBytes);
        var vectorTile = VectorTile.FromByteArray(bytes, CanonicalTileId.FromDelimitedPatternInString(pbfPath, '-'), Constants.ReadSettingsStrict);

        foreach (var (layerName, layer) in vectorTile.LayersByName)
        {
            var features = layer.FeatureGroups.EnumerateIndividualFeatures().ToList();
            foreach (var feature in features)
            {                
                var properties = feature.Properties;
                var featurePropertyKeys = properties.Keys.ToHashSet();

                if (feature.Properties.TryGetValue(stringPropToCheck, out var foundPropVal))
                {
                    var firstExistingKey = feature.Properties.FirstOrDefault(kvp => kvp.Value.Equals(foundPropVal)).Key;
                    var checkedKeyExistsInSet = featurePropertyKeys.Contains(stringPropToCheck);

                    if (firstExistingKey is not null && !checkedKeyExistsInSet)
                    {
                        Assert.That(firstExistingKey, Is.EqualTo(stringPropToCheck),
                            $"{feature} PropertyValue ({foundPropVal}) found for '{stringPropToCheck}' corresponds to key '{firstExistingKey}', expected '{stringPropToCheck}'.");
                    }
                    
                    Assert.That(checkedKeyExistsInSet, Is.True,
                        $"{feature} TryGetValue succeeded for '{stringPropToCheck}' yielding '{foundPropVal}', but key not found in properties.");
                    
                    Assert.That(foundPropVal.Kind, Is.EqualTo(ValueKind.String),
                        $"{feature} property '{stringPropToCheck}' should be of kind String, but was {foundPropVal.Kind}.");
                }
                else
                {
                    Assert.That(featurePropertyKeys.Contains(stringPropToCheck), Is.False,
                        $"{feature} TryGetValue failed for '{stringPropToCheck}', but key found in properties.");
                }
            }
        }
    }
}