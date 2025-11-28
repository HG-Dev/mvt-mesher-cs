using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MvtMesherCore.Collections;

/// <summary>
/// A collection of Vector2 coordinates, wrapping a FloatPoints instance.
/// Yields the first point again at the end to ensure a closed ring.
/// </summary>
public readonly struct FloatPointRing : IReadOnlyList<Vector2>, IEnumerable<Vector2>
{
    private readonly FloatPoints _points;
    private readonly bool _loopback;
    
    public FloatPointRing(ReadOnlyMemory<float> values)
    {
        if (values.Length % 2 != 0)
        {
            throw new ArgumentException($"{nameof(FloatPointRing)} requires even number of values, but got {values.Length}");
        }
        if (values.Length < 4)
        {
            throw new ArgumentException($"{nameof(FloatPointRing)} requires at least 2 points to form a ring, but got {values.Length >> 1} points");
        }

        _points = new FloatPoints(values);
        _loopback = !_points.IsClosedRing;
    }

    public override string ToString()
    {
        return $"ReadOnlyRing(Count={Count})";
    }
    
    public IEnumerator<Vector2> GetEnumerator()
    {
        foreach (var pt in _points)
        {
            yield return pt;
        }
        if (_loopback)
        {
            yield return _points[0];
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public int Count => (_points.Count) + (_loopback ? 1 : 0);

    public Vector2 this[int index]
    {
        get
        {
            if (index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (_loopback && index == Count - 1)
                return _points[0];

            return _points[index];
        }
    }

    public FloatPoints UnclosedPoints => _loopback ? _points : new FloatPoints(_points.RawValues.Slice(0, _points.RawValues.Length - 2));
}