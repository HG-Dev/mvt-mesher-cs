using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MvtMesherCore.Analysis.Language;
using MvtMesherCore.Mapbox;

public static class VectorTileExtensions
{
    /// <summary>
    /// Tabulates the sets of Unicode characters used in string properties
    /// whose names match the provided regular expression.
    /// </summary>
    /// <param name="tile">Source tile</param>
    /// <param name="propertyNameRegex">Regular expression to match property names</param>
    /// <param name="accumulator">Optional accumulator dictionary to add results to</param>
    /// <returns>A dictionary mapping property names to their corresponding UnicodeRangeSets</returns>
    public static Dictionary<string, UnicodeRangeSet> TabulateStringPropertyCharSets(this VectorTile tile, Regex propertyNameRegex, Dictionary<string, UnicodeRangeSet>? accumulator = null)
    {
        accumulator ??= new Dictionary<string, UnicodeRangeSet>();

        var stringPropertiesInTile = tile.Layers
            .SelectMany(layer => layer.FeatureGroups.EnumerateIndividualFeatures())
            .SelectMany(feature => feature.Properties)
            .Where(kvp => kvp.Value.Kind is MvtMesherCore.ValueKind.String)
            .Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.StringValue));

        foreach (var (key, value) in stringPropertiesInTile)
        {
            if (!propertyNameRegex.IsMatch(key) || string.IsNullOrWhiteSpace(value)) 
                continue;

            if (!accumulator.TryGetValue(key, out var rangeSet))
            {
                rangeSet = new UnicodeRangeSet();
                accumulator[key] = rangeSet;
            }

            rangeSet.AddCharactersFromSpan(value);
        }

        return accumulator;
    }
}