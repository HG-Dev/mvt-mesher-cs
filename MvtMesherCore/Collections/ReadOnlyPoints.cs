using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MvtMesherCore.Collections;

/// <summary>
/// A collection of Vector2 coordinates, stored internally as a sequence of floats.
/// </summary>
public readonly struct ReadOnlyPoints : IReadOnlyList<Vector2>
{
    public readonly ReadOnlyMemory<float> RawValues;
    
    public ReadOnlyPoints(ReadOnlyMemory<float> values)
    {
        RawValues = values;
        if (values.Length % 2 != 0)
        {
            throw new ArgumentException($"{nameof(ReadOnlyPoints)} requires even number of values, but got {values.Length}");
        }
    }
    
    public IEnumerator<Vector2> GetEnumerator()
    {
        for (int i = 1; i < RawValues.Length; i += 2)
        {
            var span = RawValues.Span;
            yield return new Vector2(span[i - 1], span[i]);
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public int Count => RawValues.Length >> 1;

    public Vector2 this[int index]
    {
        get
        {
            if (index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var span = RawValues.Span;
            int offset = index << 1;
            return new Vector2(span[offset], span[offset + 1]);
        }
    }
}