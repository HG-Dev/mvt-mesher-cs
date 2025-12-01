using System;
using System.Collections.Generic;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// Utility methods for reading MVT-specific PBF data from ReadOnlyMemory&lt;byte&gt;.
/// </summary>
public static class PbfMemoryUtility
{
    /// <summary>
    /// Enumerates all PropertyValues in a VectorTileLayer's Values field.
    /// </summary>
    /// <param name="fullMemory">VectorTileLayer memory</param>
    /// <returns>All PropertyValues in the layer</returns>
    public static IEnumerable<PropertyValue> EnumerateLayerPropertyValues(ReadOnlyMemory<byte> fullMemory)
    {
        var offset = 0;
        var index = 0;

        // Values have two tags: the value tag for a field, and then its internal type
        while (offset < fullMemory.Length && PbfSpan.TryFindNextTag(fullMemory.Span, VectorTileLayer.PbfTags.Values, ref offset))
        {
#if false
// DISABLED: Verbose logging
            var tempOffset = offset;
            var length = PbfSpan.ReadVarint(fullMemory.Span, ref tempOffset).ToUInt64();
            var valueTag = PbfSpan.ReadTag(fullMemory.Span, ref tempOffset);
            Console.Out.WriteLine($"Found value {index} at offset {offset}; LEN = {length} bytes; {PropertyValue.PbfTags.TagToValueKindMap[valueTag]}");
#endif
            var valueField = PbfMemory.ReadLengthDelimited(fullMemory, ref offset);
            var propertyValue = new PropertyValue(index++, valueField);

            yield return propertyValue;
        }
    }
}