using System;
using System.Collections;
using System.Collections.Generic;

namespace MvtMesherCore.Collections;

/// <summary>
/// A collection of <see cref="Polygon"/>s, which are themselves collections of polylines,
/// which are in turn slices of a single 1D sequence of float values.
/// Polygons are defined as one exterior ring followed by one or more interior rings;
/// thus a polygon can also be defined as a list of polylines.
/// </summary>
public readonly struct ReadOnlyPolygons : IReadOnlyList<Polygon>
{
    public readonly ReadOnlyMemory<float> RawValues;
    readonly Polygon[] _polygons; 
    
    /// <summary>
    /// Construct a read-only collection of polyline data.
    /// </summary>
    /// <param name="floats">1D collection of points of even length.</param>
    /// <param name="ringCounts">Number of rings (polylines) per polygon. The sum of this should equal the length of <see cref="pointCounts"/>.
    /// <param name="pointCounts">Number of X + Y points per polyline (coordinate count * 2)</param>
    /// <exception cref="ArgumentException">Thrown if <see cref="values"/> has odd length</exception>
    public ReadOnlyPolygons(ReadOnlyMemory<float> floats, 
        ReadOnlySpan<int> ringCountPerPolygon, 
        ReadOnlySpan<int> pointCountPerRing)
    {
        if (floats.Length % 2 != 0)
        {
            throw new ArgumentException($"{nameof(ReadOnlyPolygons)} requires even number of values, but got {floats.Length}");
        }

        RawValues = floats;

        _polygons = new Polygon[ringCountPerPolygon.Length];

        int currentFloatStartIdx = 0;
        int currentRingStartIdx = 0;
        int floatCount = 0;
        for (int polygonIdx = 0; polygonIdx < ringCountPerPolygon.Length; polygonIdx++)
        {
            var ringsToConsume = ringCountPerPolygon[polygonIdx];

            // The Vector2 point count for each ring
            var ringPointCounts = pointCountPerRing.Slice(currentRingStartIdx, ringsToConsume);
            // Values required to construct each ring
            floatCount = 0;
            foreach (var pointCount in ringPointCounts)
                floatCount += pointCount;
            floatCount <<= 1;

            _polygons[polygonIdx] = new Polygon(
                new ReadOnlyPolylines(
                    RawValues.Slice(currentFloatStartIdx, floatCount), 
                    ringPointCounts));

            currentFloatStartIdx += floatCount;
            currentRingStartIdx += ringsToConsume;
        }

        if (currentFloatStartIdx != floats.Length)
        {
            throw new ArgumentException($"{floats.Length} float values were given, but final index was {currentFloatStartIdx}");
        }

        if (currentRingStartIdx != pointCountPerRing.Length)
        {
            throw new ArgumentException($"{pointCountPerRing.Length} ring entries were given, but final index was {currentRingStartIdx}");
        }
    }
    
    public IEnumerator<Polygon> GetEnumerator()
    {
        return new Enumerator(_polygons);
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _polygons.GetEnumerator();
    }
    public int Count => _polygons.Length;
    public Polygon this[int index] => _polygons[index];


    public struct Enumerator : IEnumerator<Polygon>
    {
        private readonly Polygon[] _array;
        private int _index;
        public Enumerator(Polygon[] array) { _array = array; _index = -1; }
        public Polygon Current => _array[_index];
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++_index < _array.Length;
        public void Reset() => _index = -1;
        public void Dispose() { }
    }
}