using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MvtMesherCore.Collections;

/// <summary>
/// A collection of Vector2 coordinates, stored internally as a sequence of floats.
/// If ensureClosedRing is true, the collection will behave as a closed ring,
/// repeating the first point as the final point if necessary.
/// </summary>
public readonly struct ReadOnlyPoints : IReadOnlyList<Vector2>, IEnumerable<Vector2>
{
    public readonly ReadOnlyMemory<float> RawValues;
    public readonly bool IsClosedRing;
    private readonly bool _loopback;
    
    public ReadOnlyPoints(ReadOnlyMemory<float> values, bool ensureClosedRing = false)
    {
        RawValues = values;
        if (values.Length % 2 != 0)
        {
            throw new ArgumentException($"{nameof(ReadOnlyPoints)} requires even number of values, but got {values.Length}");
        }
        var naturalRing = this[^1] == this[0];
        _loopback = !naturalRing && ensureClosedRing;
        IsClosedRing = naturalRing || ensureClosedRing;
    }
    
    public IEnumerator<Vector2> GetEnumerator()
    {
        for (int i = 1; i < RawValues.Length; i += 2)
        {
            yield return new Vector2(RawValues.Span[i - 1], RawValues.Span[i]);
        }
        if (_loopback)
        {
            yield return new Vector2(RawValues.Span[0], RawValues.Span[1]);
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public int Count => (RawValues.Length >> 1) + (_loopback ? 1 : 0);

    public Vector2 this[int index]
    {
        get
        {
            if (index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_loopback && index == Count - 1)
                return this[0];

            var span = RawValues.Span;
            int offset = index << 1;
            return new Vector2(span[offset], span[offset + 1]);
        }
    }
}