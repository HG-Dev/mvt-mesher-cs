
using System;
using System.Collections.Generic;
using MvtMesherCore.Analysis.Language;

public static class UnicodeRangeSetExtensions
{
    public static readonly UnicodeRange Common = new(0x0020, 0x9FFF);

    public static string VisualizeRanges(this UnicodeRangeSet set, UnicodeRange range, int totalWidth = 88)
    {
        if (set == null || set.Count == 0)
        {
            throw new ArgumentException("UnicodeRangeSet is null or empty");
        }

        // Determine overall min and max
        int min = range.Start, max = range.End;

        // Normalize ranges to totalWidth
        char[] bar = new string('.', totalWidth).ToCharArray();
        foreach (var (start, end) in set)
        {
            int startPos = (int)((start - min) / (double)(max - min) * (totalWidth - 1));
            int endPos = (int)((end - min) / (double)(max - min) * (totalWidth - 1));
            for (int i = startPos; i <= endPos && i < totalWidth; i++)
            {
                bar[i] = '█';
            }
        }

        // Print visualization
        return "\x1B[4m" + new string(bar) + "\x1B[0m";
    }
}
