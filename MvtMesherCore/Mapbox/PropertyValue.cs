using System;
using System.Collections.Generic;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// An area of ReadOnlyMemory&lt;byte&gt; and also the value if varint
/// </summary>
/// <param name="index"></param>
/// <param name="kind"></param>
/// <param name="bytes"></param>
public readonly struct PropertyValue : IEquatable<PropertyValue>
{
    public static class PbfTags
    {
        public const uint Unknown = 0;
        public const uint String = 1 << 3 | (byte)WireType.Len;
        public const uint Float = 2 << 3 | (byte)WireType.Fixed32;
        public const uint Double = 3 << 3 | (byte)WireType.Fixed64;
        public const uint Int64 = 4 << 3 | (byte)WireType.Varint;
        public const uint UInt64 = 5 << 3 | (byte)WireType.Varint;
        public const uint SInt64 = 6 << 3 | (byte)WireType.Varint;
        public const uint Bool = 7 << 3 | (byte)WireType.Varint;
        
        public static readonly HashSet<PbfTag> ValidSet = [String, Float, Double, Int64, UInt64, SInt64, Bool];

        public static Dictionary<PbfTag, ValueKind> TagToValueKindMap = new()
        {
            { (String), ValueKind.String },
            { (Float), ValueKind.Float },
            { (Double), ValueKind.Double },
            { (Int64), ValueKind.Int64 },
            { (UInt64), ValueKind.UInt64 },
            { (SInt64), ValueKind.SInt64 },
            { (Bool), ValueKind.Bool }
        };
    }

    /// <summary>
    /// Create a property value record from a PBF value field.
    /// </summary>
    /// <param name="index">Index of this property in list of layer properties</param>
    /// <param name="valueField">Memory slice from which this value can be read</param>
    /// <exception cref="PbfReadFailure">Thrown if the underlying value tag is unknown</exception>
    public PropertyValue(int index, ReadOnlyMemory<byte> valueField)
    {
        var offset = 0;
        var valueTag = PbfSpan.ReadTag(valueField.Span, ref offset);
        Index = index;
        switch (valueTag)
        {
            case PbfTags.String:
                Bytes = PbfMemory.ReadLengthDelimited(valueField, ref offset);
                Kind = ValueKind.String;
                Varint = new Varint(0);
                break;
            case PbfTags.Float:
                Bytes = ReadOnlyMemory<byte>.Empty;
                Kind = ValueKind.Float;
                Varint = new Varint(PbfSpan.ReadFixed32(valueField.Span, ref offset));
                break;
            case PbfTags.Double:
                Bytes = ReadOnlyMemory<byte>.Empty;
                Kind = ValueKind.Double;
                Varint = new Varint(PbfSpan.ReadFixed64(valueField.Span, ref offset));
                break;
            case PbfTags.Int64:
            case PbfTags.UInt64:
            case PbfTags.SInt64:
            case PbfTags.Bool:
                Bytes = ReadOnlyMemory<byte>.Empty;
                Kind = (ValueKind)valueTag.FieldNumber;
                Varint = PbfSpan.ReadVarint(valueField.Span, ref offset);
                break;
            default:
                throw new PbfReadFailure($"Unexpected field number when reading Value messages: {valueTag}" +
                                        $"\n{nameof(PbfTags.ValidSet)} has {string.Join(", ", PbfTags.ValidSet)}");
        }
    }
    
    public readonly int Index;
    public readonly ValueKind Kind;
    public readonly Varint Varint;
    public readonly ReadOnlyMemory<byte> Bytes;

    public override string ToString()
    {
        return $"PropertyValue[{Index}] ({Kind}): {Varint}:{ToShortString()}";
    }

    public string ToShortString()
    {
        switch (Kind)
        {
            case ValueKind.String:
                return StringValue;
            case ValueKind.Float:
                return FloatValue.ToString();
            case ValueKind.Double:
                return DoubleValue.ToString();
            case ValueKind.Int64:
                return Int64Value.ToString();
            case ValueKind.UInt64:
                return UInt64Value.ToString();
            case ValueKind.SInt64:
                return SInt64Value.ToString();
            case ValueKind.Bool:
                return BooleanValue.ToString();
            default:
                return "Unknown";
        }
    }

    public bool Equals(PropertyValue other)
    {
        return Kind == other.Kind && Varint.Equals(other.Varint) && Bytes.Span.SequenceEqual(other.Bytes.Span);
    }

    public string StringValue => Kind == ValueKind.String 
        ? System.Text.Encoding.UTF8.GetString(Bytes.Span) 
        : throw new PbfReadFailure($"{ToString()} not a string");
    public float FloatValue => Varint.ToUInt32();
    public double DoubleValue => Varint.ToUInt64();
    public long Int64Value => Varint.ToInt64();
    public ulong UInt64Value => Varint.ToUInt64();
    public long SInt64Value => Varint.ZigZagDecode();
    public bool BooleanValue => Varint.ToBoolean();
}
