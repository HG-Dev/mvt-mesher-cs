using System;

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

    /// <summary>
    /// Deconstruct this UnicodeRange into its start and end values.
    /// </summary>
    public void Deconstruct(out int Start, out int End)
    {
        Start = this.Start;
        End = this.End;
    }
}