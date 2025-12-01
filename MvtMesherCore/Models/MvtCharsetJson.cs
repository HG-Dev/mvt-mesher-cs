using System;
using System.Collections.Generic;
using System.Linq;
using MvtMesherCore.Analysis.Language;
using Newtonsoft.Json;

namespace MvtMesherCore.Models;

/// <summary>
/// Character sets obtained from a MVT represented in JSON format for easy inspection and testing.
/// </summary>
public class MvtCharsetJson
{
    /// <summary>
    /// Tile ID in "z/x/y" format.
    /// </summary>
    [JsonProperty("tileid")]
    public string TileId = "0/0/0";

    /// <summary>
    /// Character sets for properties in the MVT.
    /// </summary>
    [JsonProperty("sets")]
    public List<MvtPropertyCharset> Sets = new();

    /// <summary>
    /// Create an MvtCharsetJson from a dictionary of property names to UnicodeRangeSets.
    /// </summary>
    public static MvtCharsetJson FromDictionary(string tileId, Dictionary<string, UnicodeRangeSet> charSets)
    {
        var result = new MvtCharsetJson
        {
            TileId = tileId,
            Sets = new List<MvtPropertyCharset>()
        };
        foreach (var (propertyName, rangeSet) in charSets)
        {
            var charset = new MvtPropertyCharset
            {
                PropertyName = propertyName,
                Ranges = rangeSet.EnumerateRangesHex().ToArray(),
                SimplifiedRange = string.Join(',', rangeSet.Simplify(8).simplifiedSet.EnumerateRangesHex())
            };
            result.Sets.Add(charset);
        }
        return result;
    }
}

/// <summary>
/// A character set for a property in an MVT layer.
/// </summary>
public class MvtPropertyCharset
{
    /// <summary>
    /// Name of the property.
    /// </summary>
    [JsonProperty("propertyName")]
    public string PropertyName = "";
    /// <summary>
    /// Ranges of Unicode code points in hexadecimal format.
    /// </summary>
    [JsonProperty("ranges")]
    public string[] Ranges = Array.Empty<string>();
    /// <summary>
    /// Simplified range representation for texture atlas generation.
    /// </summary>
    [JsonProperty("simplifiedRange")]
    public string SimplifiedRange = "";
}