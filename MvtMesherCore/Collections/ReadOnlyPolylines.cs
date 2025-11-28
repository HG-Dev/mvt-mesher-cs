using System;
using System.Collections;
using System.Collections.Generic;

namespace MvtMesherCore.Collections;

/// <summary>
/// A collection of <see cref="ReadOnlyPoints"/> Vector2 collections, stored internally as a sequence of floats.
/// </summary>
public readonly struct ReadOnlyPolylines : IReadOnlyList<ReadOnlyPoints>
{
    public readonly ReadOnlyMemory<float> RawValues;
    readonly ReadOnlyPoints[] _polylines;
    public readonly bool AllClosedRings;
    
    /// <summary>
    /// Construct a read-only collection of polyline data.
    /// </summary>
    /// <param name="floats">1D collection of points (even length)</param>
    /// <param name="pointCountPerPolyline">Number of Vector2 points per polyline (sum up to <see cref="floats"/> length / 2)</param>
    /// <exception cref="ArgumentException">Thrown if <see cref="values"/> has odd length</exception>
    public ReadOnlyPolylines(ReadOnlyMemory<float> floats, ReadOnlySpan<int> pointCountPerPolyline, bool ensureClosedRings = false)
    {
        if (floats.Length % 2 != 0)
        {
            throw new ArgumentException($"{nameof(ReadOnlyPolylines)} requires even number of values, but got {floats.Length}");
        }

        RawValues = floats;
        var totalFloatCount = 0;
        AllClosedRings = true;
        _polylines = new ReadOnlyPoints[pointCountPerPolyline.Length];
        // Populate polylines array by iterating over pointCounts
        for (int i = 0; i < pointCountPerPolyline.Length; i++)
        {
            var floatsToConsume = pointCountPerPolyline[i] << 1;

            try
            {
                _polylines[i] = new ReadOnlyPoints(RawValues.Slice(totalFloatCount, floatsToConsume), ensureClosedRings);
                // Track if all polylines are closed rings
                AllClosedRings &= _polylines[i].IsClosedRing;
            }
            catch (ArgumentOutOfRangeException outOfRangeError)
            {
                throw new ArgumentOutOfRangeException(
                    $"pointCountPerPolyline float sum ({totalFloatCount + floatsToConsume}) has " +
                    $"exceeded available floats ({floats.Length} floats) at pline[{i}]", outOfRangeError);
            }
            
            totalFloatCount += floatsToConsume;
        }

        if (totalFloatCount != floats.Length)
        {
            throw new ArgumentException($"{nameof(ReadOnlyPolylines)} requires polyline point counts to sum to {floats.Length} floats, but got {totalFloatCount} floats");
        }
    }

    public static readonly ReadOnlyPolylines Empty = new ReadOnlyPolylines(ReadOnlyMemory<float>.Empty, ReadOnlySpan<int>.Empty);

    public ReadOnlyPolylines Slice(int startIndex, int count)
    {
        if (startIndex < 0 || count < 0 || startIndex + count > _polylines.Length)
            throw new ArgumentOutOfRangeException("Invalid slice range.");

        // Compute new slice lengths
        var pointCountPerPolyline = new int[count];
        int totalFloats = 0;
        for (int i = 0; i < count; i++)
        {
            var floatCount = _polylines[startIndex + i].RawValues.Length;
            pointCountPerPolyline[i] = floatCount >> 1; // Divide by two for point count
            totalFloats += floatCount;
        }

        // Compute starting offset in RawValues
        int offset = 0;
        for (int i = 0; i < startIndex; i++)
            offset += _polylines[i].RawValues.Length;

        return new ReadOnlyPolylines(RawValues.Slice(offset, totalFloats), pointCountPerPolyline, ensureClosedRings: AllClosedRings);
    }

    public ReadOnlyPolylines Slice(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(_polylines.Length);
        return Slice(offset, length);
    }
    
    public IEnumerator<ReadOnlyPoints> GetEnumerator()
    {
        return new Enumerator(_polylines);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _polylines.GetEnumerator();
    }
    public int Count => _polylines.Length;
    public ReadOnlyPoints this[int index] => _polylines[index];


    public struct Enumerator : IEnumerator<ReadOnlyPoints>
    {
        private readonly ReadOnlyPoints[] _array;
        private int _index;
        public Enumerator(ReadOnlyPoints[] array) { _array = array; _index = -1; }
        public ReadOnlyPoints Current => _array[_index];
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _array.Length;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}