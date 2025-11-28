public enum CartesianWinding : sbyte
{
    /// <summary>
    /// Clockwise winding order for a standard Cartesian coordinate system; negative area.
    /// </summary>
    Clockwise = -1,
    /// <summary>
    /// Zero area.
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// Counter-clockwise winding order for a standard Cartesian coordinate system; positive area.
    /// </summary>
    CounterClockwise = 1
}

public enum RingType : sbyte
{
    /// <summary>
    /// Interior ring (hole) in a polygon.
    /// Mapbox Vector Tile draw on y-flipped canvas, so their interior rings appear
    /// with clockwise winding order when viewed.
    /// </summary>
    Interior = -1, // Should have negative area on standard Cartesian canvas
    /// <summary>
    /// Invalid ring type; implies an area of zero.
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// Exterior ring (outer boundary) of a polygon.
    /// Mapbox Vector Tile draw on y-flipped canvas, so their exterior rings appear
    /// with counter-clockwise winding order when viewed.
    /// </summary>
    Exterior = 1 // Should have positive area on standard Cartesian canvas
}

public static class WindingExtensions
{
    public static RingType ToAxisFlippedRingType(this CartesianWinding winding) => (RingType)winding;
}