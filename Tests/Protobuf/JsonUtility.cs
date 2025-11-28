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
        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<MvtJson>(json) ?? throw new Exception("Failed to deserialize MvtJson");
        foreach (var (layerName, layer) in result.Layers)
        {
            foreach (var feature in layer.Features)
            {
                feature.ParentLayerName = layerName;
            }
        }
        return result;
    }

    public static void AssertEquivalency(MvtMesherCore.Mapbox.VectorTile tile, MvtJson expectedJson)
    {
        Assert.That(tile.LayerNames, Is.EquivalentTo(expectedJson.Layers.Keys));

        foreach (var (layerName, layer) in tile.LayersByName)
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
                    var comparisonModel = MvtJsonFeature.FromVectorTileFeature(feature);
                    var expectedModel = expectedLayer.Features.FirstOrDefault(f => f.FeatureKey == comparisonModel.FeatureKey);
                    Assert.That(expectedModel, Is.Not.Null, $"JSON comparison model lacks feature {id} in layer {layerName}");
                    Assert.That((byte)feature.GeometryType, Is.EqualTo(expectedModel.GeometryType), $"{feature} - in layer {layerName} geometry type does not match; internal geometry class: {feature._geometry.GetType().Name}");
                    Assert.That(comparisonModel.Properties.Count, Is.EqualTo(expectedModel.Properties.Count),
                        $"{feature} in layer {layerName} property count does not match");
                    foreach (var (key, value) in expectedModel.Properties)
                    {
                        Assert.That(comparisonModel.Properties[key], Is.EqualTo(value), $"{feature} - property '{key}' does not match");
                    }
                    Assert.That(expectedModel.GeometryPoints.Count, Is.EqualTo(comparisonModel.GeometryPoints.Count),
                        $"{feature} - geometry point count does not match: expected {string.Join(", ", expectedModel.GeometryPoints)}; got {string.Join(", ", comparisonModel.GeometryPoints)}");
                    for (int i = 0; i < expectedModel.GeometryPoints.Count; i++)
                    {
                        Assert.That(comparisonModel.GeometryPoints[i], Is.EqualTo(expectedModel.GeometryPoints[i]),
                            $"{feature} - point {i} does not match");
                    }
                }
            }
        }
    }
}