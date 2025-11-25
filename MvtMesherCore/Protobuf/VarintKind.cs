namespace MvtMesherCore;

public enum VarintKind
{
    UnsignedInteger64,
    Integer64,
    SignedInteger64, // Zig zag encoded
    UnsignedInteger32,
    Integer32,
    SignedInteger32 // Zig zag encoded
}