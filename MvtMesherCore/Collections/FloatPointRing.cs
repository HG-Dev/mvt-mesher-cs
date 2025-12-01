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
    
    /// <summary>
    /// Create a FloatPointRing from a ReadOnlyMemory of float values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the input values do not have an even length</exception>
    /// <exception cref="ArgumentException">Thrown when there are not enough values to create a valid ring</exception>
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

    /// <summary>
    /// String representation of this FloatPointRing.
    /// </summary>
    public override string ToString()
    {
        return $"{nameof(FloatPointRing)}(Count={Count})";
    }
    
    /// <summary>
    /// Enumerates the points in this FloatPointRing, including the loopback point if applicable.
    /// </summary>
    /// <returns>An enumerator over the Vector2 points in the ring.</returns>
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

    /// <summary>
    /// Number of points in this FloatPointRing, including the loopback point if applicable.
    /// </summary>
    public int Count => (_points.Count) + (_loopback ? 1 : 0);

    /// <summary>
    /// Get the point at the specified index. If the index is equal to Count - 1 and the ring is not closed,
    /// this will return the first point to close the ring.
    /// </summary>
    /// <param name="index">Index of the point to retrieve.</param>
    /// <returns>The Vector2 point at the specified index.</returns>
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

    /// <summary>
    /// Gets the points excluding the loopback point.
    /// </summary>
    public FloatPoints UnclosedPoints => _loopback ? _points : new FloatPoints(_points.RawValues.Slice(0, _points.RawValues.Length - 2));
}