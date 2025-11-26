using System;
using System.Collections.Generic;
using System.Linq;

namespace MvtMesherCore;

/// <summary>
/// Provides stateless static methods for reading Mapbox Vertex Tile protobuf encoded data from a ReadOnlySpan&lt;byte&gt;.
/// </summary>
public static class PbfSpan
{
    public static Func<PbfTag, Exception?>? ExtraTagValidation;

    public static IReadOnlyCollection<PbfTag> FindInvalidTags(ReadOnlySpan<byte> span, IReadOnlyCollection<int> validFieldNumbers)
    {
        var invalid = new HashSet<PbfTag>();
        PbfTag currentTag;
        for (int offset = 0; offset < span.Length; SkipField(span, ref offset, currentTag.WireType))
        {
            currentTag = ReadTag(span, ref offset);
            if (!validFieldNumbers.Contains(currentTag.FieldNumber))
            {
                invalid.Add(currentTag);
            }
        }

        return invalid;
    }
    
    /// <summary>
    /// Reads a tag from the span, which includes the field number and wire type.
    /// </summary>
    public static PbfTag ReadTag(ReadOnlySpan<byte> span, ref int offset)
    {
        var tag = ReadVarint(span, ref offset).ToTag();
        switch (tag.FieldNumber)
        {
            case 0:
                throw new PbfReadFailure($"Tag {tag.FieldNumber} is Protobuf reserved");
            case > (1 << 29) - 1:
                throw new PbfReadFailure($"Tag {tag.FieldNumber} exceeds maximum allowed value");
            default:
                if (ExtraTagValidation?.Invoke(tag) is { } error)
                    throw error;
                break;
        }

        return tag;
    }
    
    public static bool TryFindNextTag(ReadOnlySpan<byte> span, PbfTag tag, ref int offset)
    {
        PbfTag currentTag;
        for (; offset < span.Length; SkipField(span, ref offset, currentTag.WireType))
        {
            currentTag = ReadTag(span, ref offset);
            if (currentTag.Equals(tag))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryFindFirstTag(ReadOnlySpan<byte> span, PbfTag tag, out int start)
    {
        start = 0;
        return TryFindNextTag(span, tag, ref start);
    }

    /// <summary>
    /// Reads a varint (variable-length integer) from the span.
    /// <see href="https://protobuf.dev/programming-guides/encoding/"/>
    /// </summary>
    /// <param name="span">PBF bytes</param>
    /// <param name="offset">Current offset ref to be updated</param>
    /// <returns>The decoded unsigned integer value.</returns>
    /// <exception cref="PbfReadFailure">Varint too long: field did not terminate at maximum 64 bits</exception>
    /// <exception cref="PbfReadFailure">Unexpected end of data while reading varint: field did not terminate before end of span</exception>
    public static Varint ReadVarint(ReadOnlySpan<byte> span, ref int offset)
    {
        ulong result = 0;
        int shift = 0;

        while (offset < span.Length)
        {
            byte b = span[offset++];
            result |= (ulong)(b & 0x7F) << shift;
            // Take the lower 7 bits of tbe byte; shift them into position and merge with result
            
            if ((b & 0x80) == 0)
                return new Varint(result);
            // If the most-significant-bit (bit[7]) is 0, this is the last byte of the varint. Return result
            
            shift += 7;
            if (shift > 64) throw new PbfReadFailure("Varint too long");
        }

        throw new PbfReadFailure("Unexpected end of data while reading varint");
    }
    
    public static Varint ReadVarintField(ReadOnlySpan<byte> span)
    {
        var offset = 0;
        return ReadVarint(span, ref offset);
    }

    public static List<Varint> ReadVarintPackedField(ReadOnlySpan<byte> packedSpan)
    {
        var list = new List<Varint>();
        var offset = 0;
        while (offset < packedSpan.Length)
        {
            list.Add(ReadVarint(packedSpan, ref offset));
        }
        return list;
    }

    /// <summary>
    /// Reads a 32-bit fixed-width value from the span.
    /// </summary>
    /// <returns>The uint found at offset.</returns>
    public static uint ReadFixed32(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 4 > span.Length) throw new PbfReadFailure("Not enough data to read Fixed32");
        uint value = BitConverter.ToUInt32(span.Slice(offset, 4));
        offset += 4;
        return value;
    }

    public static uint ReadFixed32Field(ReadOnlySpan<byte> span)
    {
        var offset = 0;
        return ReadFixed32(span, ref offset);
    }

    /// <summary>
    /// Reads a 64-bit fixed-width value from the span.
    /// </summary>
    /// <returns>The ulong found at offset.</returns>
    public static ulong ReadFixed64(ReadOnlySpan<byte> span, ref int offset)
    {
        if (offset + 8 > span.Length) throw new PbfReadFailure("Not enough data to read Fixed64");
        ulong value = BitConverter.ToUInt64(span.Slice(offset, 8));
        offset += 8;
        return value;
    }
    
    public static ulong ReadFixed64Field(ReadOnlySpan<byte> span)
    {
        var offset = 0;
        return ReadFixed64(span, ref offset);
    }
    
    /// <summary>
    /// Reads a UTF-8 encoded string from a length-delimited field.
    /// </summary>
    /// <returns>The decoded string.</returns>
    public static string ReadString(ReadOnlySpan<byte> span, ref int offset)
    {
        ulong length = ReadVarint(span, ref offset).ToUInt64();
        if (offset + (int)length > span.Length)
            throw new PbfReadFailure("Not enough data to read string.");

        var slice = span.Slice(offset, (int)length);
        offset += (int)length;

        return System.Text.Encoding.UTF8.GetString(slice);
    }
    
    public static List<string> ReadAllStringsWithTag(ReadOnlySpan<byte> span, PbfTag tag)
    {
        var found = new List<string>();
        var offset = 0;
        while (offset < span.Length && PbfSpan.TryFindNextTag(span, tag, ref offset))
        {
            var str = ReadString(span, ref offset);
            Console.Out.WriteLine($"Found <{str}> at offset {offset}");
            found.Add(str);
        }

        return found;
    }

    /// <summary>
    /// Skips a field of the specified wire type.
    /// </summary>
    public static void SkipField(ReadOnlySpan<byte> span, ref int offset, WireType wireType)
    {
        switch (wireType)
        {
            case WireType.Varint:
                ReadVarint(span, ref offset);
                break;
            case WireType.Len: //bytes
                ulong len = ReadVarint(span, ref offset).ToUInt64();
                offset += (int)len;
                break;
            case WireType.Fixed32:
                offset += 4;
                break;
            case WireType.Fixed64:
                offset += 8;
                break;
            default:
                throw new PbfReadFailure($"WireType {wireType} is not supported for skipping");
        }
    }
}