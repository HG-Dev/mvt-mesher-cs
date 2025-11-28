using System;
using System.Collections.Generic;

namespace MvtMesherCore.Collections;

public readonly struct FloatPolygon(FloatPointRing[] rings)
{
    public readonly ReadOnlyMemory<float> RawValues;
    readonly FloatPointRing[] _rings = rings;

    public bool HasInteriorRings => _rings.Length > 1;

    public IReadOnlyList<FloatPointRing> AllRings => _rings;
    public FloatPointRing ExteriorRing => _rings[0];
    public IReadOnlyList<FloatPointRing> InteriorRings => HasInteriorRings
        ? _rings[1..]
        : Array.Empty<FloatPointRing>();
}