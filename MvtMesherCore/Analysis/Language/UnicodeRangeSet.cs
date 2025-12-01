using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MvtMesherCore.Analysis.Language;

/// <summary>
/// A range of Unicode code points (UTF-32).
/// </summary>
public readonly struct UnicodeRange : IEquatable<UnicodeRange>
{
    /// <summary>
    /// Start of the range.
    /// </summary>
    public readonly int Start;
    /// <summary>
    /// End of the range (inclusive).
    /// </summary>
    public readonly int End;

    /// <summary>
    /// Create a UnicodeRange from start and end code points (inclusive).
    /// </summary>
    /// <param name="start">Start of the range</param>
    /// <param name="end">End of the range (inclusive)</param>
    /// <exception cref="ArgumentException">Throws when start is greater than end.</exception>
    public UnicodeRange(int start, int end)
    {
        if (start > end)
            throw new ArgumentException("Start cannot be greater than End.");
        Start = start;
        End = end;
    }

    /// <summary>
    /// Return a string representation of the Unicode range in hexadecimal format.
    /// </summary>
    public override string ToString()
    {
        if (Start == End)
            return $"{Start:X4}"; // Single code point
        return $"{Start:X4}-{End:X4}";
    }

    /// <summary>
    /// Return a string representation of the Unicode range in decimal format.
    /// </summary>
    public string ToDecimalString()
    {
        if (Start == End)
            return $"{Start}"; // Single code point
        return $"{Start}-{End}";
    }

    /// <summary>
    /// Try to merge two Unicode ranges if they overlap or are adjacent.
    /// </summary>
    /// <param name="a">First Unicode range</param>
    /// <param name="b">Second Unicode range</param>
    /// <param name="merged">Merged Unicode range if successful</param>
    /// <returns>True if the ranges were merged; otherwise, false.</returns>
    public static bool TryMerge(UnicodeRange a, UnicodeRange b, out UnicodeRange merged)
    {
        if (b.Start <= a.End + 1 && a.Start <= b.End + 1)
        {
            merged = new UnicodeRange(Math.Min(a.Start, b.Start), Math.Max(a.End, b.End));
            return true;
        }
        merged = default;
        return false;
    }

    /// <summary>
    /// Determine if this UnicodeRange is equal to another.
    /// </summary>
    public bool Equals(UnicodeRange other)
    {
        return this.Start == other.Start && this.End == other.End;
    }
}

/// <summary>
/// A collection of UTF-32 character ranges.
/// </summary>
/// <remarks>
/// The collection automatically merges overlapping and adjacent ranges.
/// Operations are not thread-safe.
/// </remarks>
public class UnicodeRangeSet : IReadOnlyList<UnicodeRange>, IEquatable<UnicodeRangeSet>
{
    /// <summary>
    /// Internal list of ranges. Should always be kept merged and sorted.
    /// </summary>
    private readonly List<UnicodeRange> _ranges = new List<UnicodeRange>();

    /// <summary>
    /// Number of ranges in the set.
    /// </summary>
    public int Count => ((IReadOnlyCollection<UnicodeRange>)_ranges).Count;
    /// <summary>
    /// Total number of code points in all ranges.
    /// </summary>
    public int CodePointCount
    {
        get
        {
            int total = 0;
            foreach (var range in _ranges)
            {
                total += (range.End - range.Start + 1);
            }
            return total;
        }
    }

    /// <summary>
    /// Create a UnicodeRangeSet from a span of characters (surrogate pairs will be combined).
    /// </summary>
    /// <param name="span">Starting string input</param>
    /// <returns>A new UnicodeRangeSet containing the characters from the span</returns>
    public static UnicodeRangeSet FromSpan(ReadOnlySpan<char> span)
    {
        var set = new UnicodeRangeSet();
        set.AddCharactersFromSpan(span);
        return set;
    }

    /// <summary>
    /// Add a single code point (UTF-32).
    /// </summary>
    /// <param name="codePoint">32-bit Unicode code point</param>
    /// <returns>If the code point already exists in the set, returns false; otherwise, true.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws if the code point is outside the valid Unicode range.</exception>
    public bool Add(int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF)
            throw new ArgumentOutOfRangeException("Code point must be between U+0000 and U+10FFFF.");

        return TryAddCodePoint(codePoint);
    }

    /// <summary>
    /// Add a single UTF-16 character.
    /// The character will be added as its code point value.
    /// </summary>
    /// <param name="character">16-bit UTF-16 character</param>
    /// <returns>If the code point already exists in the set, returns false; otherwise, true.</returns>
    public bool AddCharacter(char character)
    {
        return TryAddCodePoint((int)character);
    }

    /// <summary>
    /// Add a range of code points from a string (surrogate pairs will be combined).
    /// </summary>
    /// <param name="span"></param>
    /// <exception cref="ArgumentException"></exception>
    public int AddCharactersFromSpan(ReadOnlySpan<char> span)
    {
        var added = 0;
        for (int i = 0; i < span.Length; i++)
        {
            int codePoint;

            if (char.IsHighSurrogate(span[i]))
            {
                if (i + 1 < span.Length && char.IsLowSurrogate(span[i + 1]))
                {
                    codePoint = char.ConvertToUtf32(span[i], span[i + 1]);
                    i++; // Skip the low surrogate
                }
                else
                {
                    throw new ArgumentException("Invalid surrogate pair in input span.");
                }
            }
            else
            {
                codePoint = span[i];
            }

            if (Add(codePoint))
                added++;
        }
        return added;
    }

    /// <summary>
    /// Try to add a single code point to the set.
    /// If the addition causes merging of existing ranges, those will be merged.
    /// </summary>
    /// <param name="codePoint"></param>
    /// <returns>True if the code point was added; false if it already existed.</returns>
    private bool TryAddCodePoint(int codePoint)
    {
        for (int i = 0; i < _ranges.Count; i++)
        {
            var range = _ranges[i];
            if (codePoint < range.Start - 1)
            {
                // Insert new range before current
                _ranges.Insert(i, new UnicodeRange(codePoint, codePoint));
                return true;
            }
            else if (codePoint == range.Start - 1)
            {
                // Extend current range at the start
                _ranges[i] = new UnicodeRange(codePoint, range.End);
                
                // Check for possible merge with previous range
                if (i > 0 && UnicodeRange.TryMerge(_ranges[i - 1], _ranges[i], out var merged))
                {
                    _ranges[i - 1] = merged;
                    _ranges.RemoveAt(i);
                }
                return true;
            }
            else if (codePoint >= range.Start && codePoint <= range.End)
            {
                // Already exists
                return false;
            }
            else if (codePoint == range.End + 1)
            {
                // Extend current range at the end
                _ranges[i] = new UnicodeRange(range.Start, codePoint);

                // Check for possible merge with next range
                if (i + 1 < _ranges.Count && UnicodeRange.TryMerge(_ranges[i], _ranges[i + 1], out var merged))
                {
                    _ranges[i] = merged;
                    _ranges.RemoveAt(i + 1);
                }
                return true;
            }
        }

        // Add new range at the end
        _ranges.Add(new UnicodeRange(codePoint, codePoint));
        return true;
    }

    /// <summary>
    /// Simplify the UnicodeRangeSet by merging ranges that are within the specified tolerance.
    /// </summary>
    /// <param name="tolerance">Range in code points within which to merge adjacent ranges.</param>
    /// <returns>A new UnicodeRangeSet with merged ranges and the number of merges performed.</returns>
    public (UnicodeRangeSet simplifiedSet, int numMerged) Simplify(int tolerance)
    {
        var set = new UnicodeRangeSet();
        var numMerged = 0;

        foreach (var range in _ranges)
        {
            if (set.Count == 0)
            {
                set._ranges.Add(range);
            }
            else
            {
                var lastRange = set._ranges[set.Count - 1];
                if (range.Start - lastRange.End <= tolerance + 1)
                {
                    // Merge ranges
                    set._ranges[set.Count - 1] = new UnicodeRange(lastRange.Start, range.End);
                    numMerged++;
                }
                else
                {
                    set._ranges.Add(range);
                }
            }
        }

        return (set, numMerged);
    }

#region IReadOnlyList Implementation
    /// <summary>
    /// Get the UnicodeRange at the specified index.
    /// </summary>
    public UnicodeRange this[int index] => ((IReadOnlyList<UnicodeRange>)_ranges)[index];

    /// <summary>
    /// Enumerate the ranges as strings in hexadecimal format.
    /// </summary>
    public IEnumerable<string> EnumerateRangesHex()
    {
        return _ranges.Select(r => r.ToString());
    }

    /// <summary>
    /// Enumerate the ranges as strings in decimal format.
    /// </summary>
    public IEnumerable<string> EnumerateRangesDecimal()
    {
        return _ranges.Select(r => r.ToDecimalString());
    }

    /// <summary>
    /// Get an enumerator for the UnicodeRangeSet.
    /// </summary>
    public IEnumerator<UnicodeRange> GetEnumerator()
    {
        return ((IEnumerable<UnicodeRange>)_ranges).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_ranges).GetEnumerator();
    }
#endregion IReadOnlyList Implementation

    /// <summary>
    /// Determine if this UnicodeRangeSet is equal to another.
    /// </summary>
    public bool Equals(UnicodeRangeSet other)
    {
        for (int i = 0; i < Count; i++)
        {
            if (!this[i].Equals(other[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Combine this UnicodeRangeSet with another.
    /// </summary>
    /// <remarks>
    /// This modifies the current set to include all ranges from the other set.
    /// TODO: Optimize this method to avoid repeated additions.
    /// </remarks>
    public void UnionWith(UnicodeRangeSet other)
    {
        foreach (var range in other._ranges)
        {
            for (int cp = range.Start; cp <= range.End; cp++)
            {
                Add(cp);
            }
        }
    }

    /// <summary>
    /// Determine if this UnicodeRangeSet is a subset of another.
    /// </summary>
    public bool IsSubsetOf(UnicodeRangeSet other)
    {
        foreach (var range in _ranges)
        {
            bool found = false;
            foreach (var otherRange in other._ranges)
            {
                if (range.Start >= otherRange.Start && range.End <= otherRange.End)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }
        return true;
    }
}