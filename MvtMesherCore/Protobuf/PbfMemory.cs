using System;
using System.Collections.Generic;

namespace MvtMesherCore;

public static class PbfMemory
{
    /// <summary>
    /// Reads a length-delimited field from ReadOnlyMemory.
    /// </summary>
    /// <returns>A memory slice with length defined by the varint at the beginning of the field.</returns>
    public static ReadOnlyMemory<byte> ReadLengthDelimited(ReadOnlyMemory<byte> memory, ref int offset)
    {
        ulong length = PbfSpan.ReadVarint(memory.Span, ref offset).ToUInt64();
        if (offset + (int)length > memory.Length) 
            throw new PbfReadFailure($"Not enough data to read LengthDelimited field: requested {length} bytes, but only {memory.Length - offset} available");
        var slice = memory.Slice(offset, (int)length);
        offset += (int)length;
        return slice;
    }

    public static ReadOnlyMemory<byte> FindFieldWithTag(ReadOnlyMemory<byte> memory, PbfTag tag)
    {
        PbfSpan.TryFindFirstTag(memory.Span, tag, out int start);
        return ReadLengthDelimited(memory, ref start);
    }

    public static IEnumerable<ReadOnlyMemory<byte>> FindAndEnumerateFieldsWithTag(ReadOnlyMemory<byte> memory, PbfTag tag)
    {
        int offset = 0;
        while (offset < memory.Length && PbfSpan.TryFindNextTag(memory.Span, tag, ref offset))
        {
            yield return ReadLengthDelimited(memory, ref offset);
        }
    }
}