namespace MvtMesherCore;

/// <summary>
/// The "wire type" is a small integer (3 bits) that specifies the encoding format of a field;
/// i.e. how a sequence of bytes should be interpreted.
/// https://protobuf.dev/programming-guides/encoding/
/// </summary>
/// <remarks>
/// "Undefined" was added to mirror MVT protobuf schema.
/// </remarks>
public enum WireType : byte
{
    /// <summary>
    /// Any type derived from a 64-bit integer of indeterminate size.
    /// </summary>
    Varint = 0,
    /// <summary>
    /// Any type derived from a 64-bit integer of fixed size.
    /// </summary>
    Fixed64 = 1,
    /// <summary>
    /// Length of strings, bytes, embedded messages, or packed repeated fields.
    /// </summary>
    /// <remarks>Known as "BYTES" in MVT schema.</remarks>
    Len = 2,
    /// <summary>
    /// Any type derived from a 32-bit integer of fixed size.
    /// </summary>
    Fixed32 = 5,
    /// <summary>
    /// Unofficial value for identifying an unknown wire type.
    /// </summary>
    Undefined = 99, // 0x00000063
}