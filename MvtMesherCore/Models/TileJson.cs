using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MvtMesherCore.Models;

/// <summary>
/// A JSON document providing an API endpoint for downloading map tiles,
/// plus some metadata concerning the map available through the endpoint.
/// </summary>
/// <param name="Name">Name of the map</param>
/// <param name="Description">Description of the map</param>
/// <param name="Version">Version of the map</param>
/// <param name="TileJsonVersion">Version of this TileJSON document</param>
/// <param name="ApiZxyTemplateUrl">API endpoint to access for tile data</param>
/// <param name="Layers">All the layers we can expect to find in tile data</param>
/// <param name="Bounds">The latitude/longitude rect for which tile data can be returned</param>
/// <param name="MaxZoom">Maximum zoom level for which data exists. No tiles of increased detail exist above this level</param>
/// <param name="MinZoom">Minimum zoom level for which data exists. No tiles of decreased detail exist below this level</param>
/// <param name="Attribution">Sources used to produce the given API's map tile data</param>
public record TileJson(string Name, string Description, string Version, string TileJsonVersion, 
    string ApiZxyTemplateUrl, TileJson.LayerDef[] Layers, MapBounds Bounds, byte MaxZoom, byte MinZoom, string Attribution)
{
    [JsonConstructor]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    public TileJson(string tilejson, string[] tiles, LayerDef[] vector_layers, string attribution,
        decimal[] bounds, decimal[] center, string description, int maxzoom, int minzoom,
        string name, string version)
        : this(name, description, version, tilejson, 
            tiles.FirstOrDefault() ?? "", vector_layers, 
            new MapBounds(bounds), (byte)maxzoom, (byte)minzoom, attribution)
    {
    }
    
    /// <summary>
    /// Overview of a layer as defined in a TileJSON document.
    /// </summary>
    /// <param name="Id">Unique name of layer</param>
    /// <param name="FieldInfo">Field name / data type pairs such as {"class", "String"} or {"ele", "Number"}</param>
    /// <param name="MinZoom">Lowest zoom level (highest elevation) at which this layer possesses data</param>
    /// <param name="MaxZoom">Highest zoom level (lowest elevation) at which this layer possesses data</param>
    public record LayerDef(string Id, Dictionary<string, string> FieldInfo, byte MinZoom, byte MaxZoom)
    {
        [JsonConstructor]
        public LayerDef(string id, Dictionary<string, string> fields, int minzoom, int maxzoom) 
            : this(id, fields, (byte)minzoom, (byte)maxzoom)
        { }
    }
}