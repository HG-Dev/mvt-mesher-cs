using System;
using System.Text.RegularExpressions;

namespace MvtMesherCore.Models;

/// <summary>
/// ZXY grid coordinates on a WGS 84 
/// <see href="https://en.wikipedia.org/wiki/Web_Mercator">Web Mercator projection</see>.
/// </summary>
/// <param name="Z">Zoom level (0 to 25)</param>
/// <param name="X">X coordinate</param>
/// <param name="Y">Y coordinate</param>
public record CanonicalTileId(byte Z, int X, int Y)
{
    /// <summary>
    /// Serializable child type for use in Unity editor.
    /// </summary>
    [Serializable]
    public struct SerializedValues
    {
        /// <summary> The zoom level. </summary>
        public int z;

        /// <summary> The X coordinate in the tile grid. </summary>
        public int x;

        /// <summary> The Y coordinate in the tile grid. </summary>
        public int y;
		
        public CanonicalTileId ToReadOnly() => new((byte)z,x,y);
    }
    
    public string ToShortString() => $"{Z}/{X}/{Y}";
    
    public static CanonicalTileId FromDelimitedPatternInString(string input, char delimiter = '/')
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));

        // Regex to match /Z/X/Y where Z, X, Y are integers
        var match = Regex.Match(input, @$"(\d+){delimiter}(\d+){delimiter}(\d+)");
        if (!match.Success)
            throw new FormatException($"Input does not contain a valid Z/X/Y pattern: {input}");

        byte z = byte.Parse(match.Groups[1].Value);
        int x = int.Parse(match.Groups[2].Value);
        int y = int.Parse(match.Groups[3].Value);

        return new CanonicalTileId(z, x, y);
    }

}