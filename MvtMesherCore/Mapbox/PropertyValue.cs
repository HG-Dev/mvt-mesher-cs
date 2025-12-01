using System;
using System.Collections.Generic;

namespace MvtMesherCore.Mapbox;

/// <summary>
/// Either: an area of ReadOnlyMemory&lt;byte&gt; (string) or a Varint and its intended ValueKind.
/// </summary>
public readonly struct PropertyValue : IEquatable<PropertyValue>
{
    /// <summary>
    /// PBF tags for PropertyValue fields.
    /// </summary>
    public static class PbfTags
    {
        /// <summary>
        /// Unknown tag value.
        /// </summary>
        public const uint Unknown = 0;
        /// <summary>
        /// String tag value with Length Delimited wire type.
        /// </summary>
        public const uint String = 1 << 3 | (byte)WireType.Len;
        /// <summary>
        /// Float tag value with Fixed32 wire type.
        /// </summary>
        public const uint Float = 2 << 3 | (byte)WireType.Fixed32;
        /// <summary>
        /// Double tag value with Fixed64 wire type.
        /// </summary>
        public const uint Double = 3 << 3 | (byte)WireType.Fixed64;
        /// <summary>
        /// Int64 tag value with Varint wire type.
        /// </summary>
        public const uint Int64 = 4 << 3 | (byte)WireType.Varint;
        /// <summary>
        /// UInt64 tag value with Varint wire type.
        /// </summary>
        public const uint UInt64 = 5 << 3 | (byte)WireType.Varint;
        /// <summary>
        /// SInt64 tag value with Varint wire type.
        /// </summary>
        public const uint SInt64 = 6 << 3 | (byte)WireType.Varint;
        /// <summary>
        /// Bool tag value with Varint wire type.
        /// </summary>
        public const uint Bool = 7 << 3 | (byte)WireType.Varint;
        
        /// <summary>
        /// Set of all valid PropertyValue tags.
        /// </summary>
        public static readonly HashSet<PbfTag> ValidSet = [String, Float, Double, Int64, UInt64, SInt64, Bool];

        /// <summary>
        /// Mapping from PBF tags to ValueKind enum values.
        /// </summary>
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
    
    /// <summary>
    /// Index of this property in list of layer properties.
    /// </summary>
    /// <remarks>
    /// Layers possess a list of PropertyValues in their 'values' field.
    /// This index is used for dereferencing from said list.
    /// </remarks>
    public readonly int Index;
    /// <summary>
    /// Kind of value stored in this PropertyValue, as determined by its PBF tag.
    /// </summary>
    public readonly ValueKind Kind;
    /// <summary>
    /// Varint representation of this value, if applicable.
    /// </summary>
    public readonly Varint Varint;
    /// <summary>
    /// Raw bytes of this value, if applicable.
    /// </summary>
    public readonly ReadOnlyMemory<byte> Bytes;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"PropertyValue[{Index}] ({Kind}): {Varint}:{ToShortString()}";
    }

    /// <summary>
    /// Gets a minimal string representation of the value.
    /// For strings, returns the string itself; for Varint derivatives, returns the numeric value.
    /// </summary>
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

    /// <summary>
    /// Checks equality between this PropertyValue and another.
    /// </summary>
    public bool Equals(PropertyValue other)
    {
        return Kind == other.Kind && Varint.Equals(other.Varint) && Bytes.Span.SequenceEqual(other.Bytes.Span);
    }

    /// <summary>
    /// Read the string value of this PropertyValue.
    /// </summary>
    public string StringValue => Kind == ValueKind.String 
        ? System.Text.Encoding.UTF8.GetString(Bytes.Span) 
        : throw new PbfReadFailure($"{ToString()} not a string");
    /// <summary>
    /// Read the Varint of this PropertyValue as a float.
    /// </summary>
    public float FloatValue => Varint.ToUInt32();
    /// <summary>
    /// Read the Varint of this PropertyValue as a double.
    /// </summary>
    public double DoubleValue => Varint.ToUInt64();
    /// <summary>
    /// Read the Varint of this PropertyValue as a signed 64-bit integer.
    /// </summary>
    public long Int64Value => Varint.ToInt64();
    /// <summary>
    /// Read the Varint of this PropertyValue as an unsigned 64-bit integer.
    /// </summary>
    public ulong UInt64Value => Varint.ToUInt64();
    /// <summary>
    /// Read the Varint of this PropertyValue as a signed 64-bit integer using ZigZag decoding.
    /// </summary>
    public long SInt64Value => Varint.ZigZagDecode();
    /// <summary>
    /// Read the Varint of this PropertyValue as a boolean.
    /// </summary>
    public bool BooleanValue => Varint.ToBoolean();
}
