using System;

namespace MvtMesherCore;

public readonly struct Varint(ulong value) : IEquatable<Varint>, IEquatable<ulong>
{
    readonly ulong _value = value;
    
    public long ToInt64() => (long)_value; // For plain int64 fields
    public static readonly Converter<Varint, long> Int64Conversion = vi => vi.ToInt64();  
    public ulong ToUInt64() => _value;
    public static readonly Converter<Varint, ulong> UInt64Conversion = vi => vi.ToUInt64();
    public int ToInt32() => (int)_value;
    public static readonly Converter<Varint, int> Int32Conversion = vi => vi.ToInt32();
    public uint ToUInt32() => (uint)_value;
    public static readonly Converter<Varint, uint> UInt32Conversion = vi => vi.ToUInt32();
    public bool ToBoolean() => _value switch { 1 => true, 0 => false, 
        _ => throw new IndexOutOfRangeException($"Protobuf boolean varints should be 0 or 1. Encountered {_value}") };
    public static readonly Converter<Varint, bool> BooleanConversion = vi => vi.ToBoolean();
    
    public PbfTag ToTag() => ToUInt32();
    
    /// <summary>
    /// Decode the internal ulong value as a zig-zag encoded signed value.
    /// </summary>
    /// <returns>Signed long</returns>
    public long ZigZagDecode()
    {
        // Zigzag decoding for sint64 (or sint32)
        return ((long)_value >> 1) ^ -(long)(_value & 1);
    }
    public static readonly Converter<Varint, long> SInt64Conversion = vi => vi.ZigZagDecode();

    public bool Equals(Varint other)
    {
        return other._value == _value;
    }

    public bool Equals(ulong other)
    {
        return _value == other;
    }

    public override string ToString() => _value.ToString();
}
