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
            var tempOffset = offset;
            var length = PbfSpan.ReadVarint(fullMemory.Span, ref tempOffset).ToUInt64();
            var valueTag = PbfSpan.ReadTag(fullMemory.Span, ref tempOffset);
            Console.Out.WriteLine($"Found value {index} at offset {offset}; LEN = {length} bytes; {PropertyValue.PbfTags.TagToValueKindMap[valueTag]}");
            var valueField = PbfMemory.ReadLengthDelimited(fullMemory, ref offset);
            var propertyValue = new PropertyValue(index++, valueField);
            Console.Out.WriteLine($"-> {propertyValue.ToString()}");
            yield return propertyValue;
        }
    }
}