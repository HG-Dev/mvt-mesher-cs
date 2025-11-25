using MvtMesherCore.Models;
using Newtonsoft.Json;

namespace Tests.Json;

[TestFixture]
public class ModelTests
{
    public const string CorrectTileJsonFilePath = "Res/planet_tilejson.json";
    
    [Test]
    public void VectorLayerJsonShouldCovert()
    {
        string json = """
                      {
                        "id": "aeroway",
                        "fields": {
                          "class": "String",
                          "ref": "String"
                        },
                        "minzoom": 10,
                        "maxzoom": 14
                      }
                      """;
        var layer = JsonConvert.DeserializeObject<TileJson.LayerDef>(json);
        Assert.That(layer, Is.Not.Null);
        Assert.That(layer.Id, Is.EqualTo("aeroway"));
        Assert.That(layer.FieldInfo, Has.Count.EqualTo(2));
        Assert.That(layer.MinZoom, Is.EqualTo(10));
        Assert.That(layer.MaxZoom, Is.EqualTo(14));
    }

    [Test]
    public void MapBoundsJsonShouldCovert()
    {
        string json = "{ bounds: [-180, -85.05113, 180, 85.05113] }";
        var bounds = JsonConvert.DeserializeObject<MapBounds>(json);
        Assert.That(bounds, Is.Not.Null);
        Assert.That(bounds.MinLatitude, Is.EqualTo(-180));
        Assert.That(bounds.MinLongitude, Is.EqualTo(-85.05113));
        Assert.That(bounds.MaxLatitude, Is.EqualTo(180));
        Assert.That(bounds.MaxLongitude, Is.EqualTo(85.05113));
    }

    [TestCase(CorrectTileJsonFilePath)]
    public void ValidJsonShouldConvertIntoTileJsonRecord(string filepath)
    {
        using StreamReader file = File.OpenText(filepath);
        using JsonReader reader = new JsonTextReader(file);
        var deserializer = new JsonSerializer();
        var tileJson = deserializer.Deserialize<TileJson>(reader);
        Assert.That(tileJson, Is.Not.Null);
        Assert.That(tileJson.TileJsonVersion, Is.EqualTo("3.0.0"));
        Assert.That(tileJson.ApiZxyTemplateUrl, Is.EqualTo("https://tiles.openfreemap.org/planet/20251112_001001_pt/{z}/{x}/{y}.pbf"));
        Assert.That(tileJson.Layers, Has.Length.EqualTo(16));
        Assert.That(tileJson.Bounds, Is.Not.Default);
    }
}