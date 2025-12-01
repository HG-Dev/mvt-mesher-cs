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
public readonly struct FloatPoints : IReadOnlyList<Vector2>, IEnumerable<Vector2>
{
    /// <summary>
    /// Raw float values representing the points as [x0, y0, x1, y1, ..., xN, yN].
    /// </summary>
    public readonly ReadOnlyMemory<float> RawValues;
    /// <summary>
    /// Indicates whether the points form a closed ring (first point equals last point).
    /// </summary>
    public readonly bool IsClosedRing;
    
    /// <summary>
    /// Create a FloatPoints from a ReadOnlyMemory of float values.
    /// </summary>
    public FloatPoints(ReadOnlyMemory<float> values)
    {
        RawValues = values;
        if (values.Length % 2 != 0)
        {
            throw new ArgumentException($"{nameof(FloatPoints)} requires even number of values, but got {values.Length}");
        }

        if (values.Length < 2)
        {
            IsClosedRing = false;
            return;
        }

        var first = new Vector2(values.Span[0], values.Span[1]);
        var last = new Vector2(values.Span[^2], values.Span[^1]);

        IsClosedRing = first == last;
    }
    
    /// <summary>
    /// Enumerates the points in this FloatPoints collection.
    /// </summary>
    /// <returns>An enumerator over the Vector2 points.</returns>
    public IEnumerator<Vector2> GetEnumerator()
    {
        for (int i = 1; i < RawValues.Length; i += 2)
        {
            yield return new Vector2(RawValues.Span[i - 1], RawValues.Span[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Number of points in this FloatPoints collection.
    /// </summary>
    public int Count => RawValues.Length >> 1;

    /// <summary>
    /// Gets the point at the specified index.
    /// </summary>
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

    /// <summary>
    /// Gets a slice of the FloatPoints collection.
    /// </summary>
    public FloatPoints this[Range range] => Slice(range);

    /// <summary>
    /// Gets a slice of the FloatPoints collection.
    /// </summary>
    public FloatPoints Slice(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(Count);
        return Slice(offset, length);
    }

    /// <summary>
    /// Gets a slice of the FloatPoints collection.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when slice range is invalid</exception>
    public FloatPoints Slice(int startIndex, int count)
    {
        if (startIndex < 0 || count < 0 || startIndex + count > Count)
            throw new ArgumentOutOfRangeException("Invalid slice range.");

        return new FloatPoints(RawValues.Slice(startIndex << 1, count << 1));
    }
}