namespace MvtMesherCore;

/// <summary>
/// The kind of value stored in a PropertyValue.
/// </summary>
public enum ValueKind : byte
{
    /// <summary>
    /// Unknown value.
    /// </summary>
    Unknown,
    /// <summary>
    /// String value.
    /// </summary>
    String,
    /// <summary>
    /// Varint - float value.
    /// </summary>
    Float,
    /// <summary>
    /// Varint - double value.
    /// </summary>
    Double,
    /// <summary>
    /// Varint - signed 64-bit integer value.
    /// </summary>
    Int64,
    /// <summary>
    /// Varint - unsigned 64-bit integer value.
    /// </summary>
    UInt64,
    /// <summary>
    /// Varint - signed 64-bit integer value kind using ZigZag encoding.
    /// </summary>
    SInt64,
    /// <summary>
    /// Varint - boolean value.
    /// </summary>
    Bool
}