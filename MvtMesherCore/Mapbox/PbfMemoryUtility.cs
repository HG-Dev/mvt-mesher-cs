using System;
using System.Collections.Generic;

namespace MvtMesherCore.Mapbox;

public static class PbfMemoryUtility
{
    public static IEnumerable<PropertyValue> EnumeratePropertyValuesWithTag(ReadOnlyMemory<byte> fullMemory, PbfTag tag)
    {
        var offset = 0;
        var index = 0;

        // Values have two tags: the value tag for a field, and then its internal type
        while (offset < fullMemory.Length && PbfSpan.TryFindNextTag(fullMemory.Span, tag, ref offset))
        {
            var valueField = PbfMemory.ReadLengthDelimited(fullMemory, ref offset);
            yield return new PropertyValue(index++, valueField);
        }
    }
}