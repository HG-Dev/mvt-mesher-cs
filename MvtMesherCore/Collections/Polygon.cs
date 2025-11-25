using System;
using MvtMesherCore.Collections;

public readonly struct Polygon(ReadOnlyPolylines rings)
{
    public readonly ReadOnlyMemory<float> RawValues;
    readonly ReadOnlyPolylines _rings = rings;

    public bool HasInteriorRings => _rings.Count > 1;

    public ReadOnlyPoints ExteriorRing => _rings[0];
    public ReadOnlyPolylines InteriorRings => HasInteriorRings
        ? _rings.Slice(1..)
        : default;
}