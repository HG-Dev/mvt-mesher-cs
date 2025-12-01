using System;
using System.Collections.Generic;

namespace MvtMesherCore.Collections;

/// <summary>
/// A polygon represented as an array of FloatPointRings.
/// </summary>
public readonly struct FloatPolygon(FloatPointRing[] rings)
{
    /// <summary>
    /// Raw float values of all rings in this polygon.
    /// </summary>
    public readonly ReadOnlyMemory<float> RawValues;

    readonly FloatPointRing[] _rings = rings;

    /// <summary>
    /// Indicates whether this polygon has interior rings.
    /// </summary>
    public bool HasInteriorRings => _rings.Length > 1;

    /// <summary>
    /// All rings in this polygon.
    /// </summary>
    public IReadOnlyList<FloatPointRing> AllRings => _rings;

    /// <summary>
    /// The exterior ring of this polygon.
    /// </summary>
    // TODO: Add winding validation
    public FloatPointRing ExteriorRing => _rings[0];

    /// <summary>
    /// The interior rings of this polygon.
    /// </summary>
    public IReadOnlyList<FloatPointRing> InteriorRings => HasInteriorRings
        ? _rings[1..]
        : Array.Empty<FloatPointRing>();
}