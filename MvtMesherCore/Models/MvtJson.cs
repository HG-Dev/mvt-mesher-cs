using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MvtMesherCore.Models;

/// <summary>
/// An MVT represented in JSON format for easy inspection and testing.
/// </summary>
public class MvtJson
{
    [JsonProperty("layers")]
    public Dictionary<string, MvtJsonLayer> Layers = new();
    [JsonProperty("tileid")]
    public string TileId = "0/0/0";
}

public class MvtJsonLayer
{
    [JsonProperty("version")]
    public ulong Version;
    [JsonProperty("extent")]
    public ulong Extent;
    [JsonProperty("propertyNames")]
    public List<string> PropertyNames = new();
    [JsonProperty("propertyValues")]
    public List<string> PropertyValues = new();
    [JsonProperty("features")]
    public List<MvtJsonFeature> Features = new();
}

public class MvtJsonFeature
{
    [JsonProperty("id")]
    public ulong Id;
    [JsonProperty("geometryType")]
    public byte GeometryType;
    [JsonProperty("properties")]
    public Dictionary<string, string> Properties = new();
}