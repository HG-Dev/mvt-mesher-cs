using MvtMesherCore.Models;
using MvtMesherCore.Mapbox;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.Protobuf;

public static class JsonUtility
{
    public static MvtJson LoadAsMvtJson(this string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<MvtJson>(json) ?? throw new Exception("Failed to deserialize MvtJson");
    }

    public static void AssertEquivalency(MvtMesherCore.Mapbox.VectorTile tile, MvtJson expectedJson)
    {
        Assert.That(tile.LayerNames, Is.EquivalentTo(expectedJson.Layers.Keys));

        foreach (var (layerName, layer) in tile)
        {
            var expectedLayer = expectedJson.Layers[layerName];
            // Compare property names, values, features, version, extent
            Assert.That(layer.PropertyNames, Is.EquivalentTo(expectedLayer.PropertyNames), $"Layer {layerName} property names do not match");
            Assert.That(layer.PropertyValues.Select(v => v.ToShortString()), Is.EquivalentTo(expectedLayer.PropertyValues), $"Layer {layerName} property values do not match");
            Assert.That(layer.FeatureGroups.ItemCount, Is.EqualTo(expectedLayer.Features.Count), $"Layer {layerName} feature count does not match");
            Assert.That(layer.Version, Is.EqualTo(expectedLayer.Version), $"Layer {layerName} version does not match");
            Assert.That(layer.Extent, Is.EqualTo(expectedLayer.Extent), $"Layer {layerName} extent does not match");

            foreach (var (id, features) in layer.FeatureGroups)
            {
                foreach (var feature in features)
                {
                    var expectedFeature = expectedLayer.Features.FirstOrDefault(f => f.Id == id && f.GeometryType == (byte)feature.GeometryType);
                    Assert.That(expectedFeature, Is.Not.Null, $"JSON comparison model lacks feature {id} in layer {layerName}");
                    Assert.That((byte)feature.GeometryType, Is.EqualTo(expectedFeature.GeometryType), $"Feature {id} in layer {layerName} geometry type does not match; internal geometry class: {feature._geometry.GetType().Name}");
                    foreach (var (key, value) in expectedFeature.Properties)
                    {
                        Assert.That(feature.Properties[key].ToShortString(), Is.EqualTo(value), $"Feature {id} in layer {layerName} property '{key}' does not match");
                    }
                }
            }
        }
    }
}