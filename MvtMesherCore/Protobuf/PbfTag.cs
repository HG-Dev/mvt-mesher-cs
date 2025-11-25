namespace MvtMesherCore;

public record PbfTag
{
    readonly uint _value;
    public int FieldNumber => (int)(_value >> 3);
    public WireType WireType => (WireType)(_value & 0x07);

    public PbfTag(uint value)
    {
        _value = value;
    }
    
    public PbfTag(int fieldNumber, WireType wireType) : this((uint)(fieldNumber << 3 | (byte)wireType))
    {
    }

    public static implicit operator uint(PbfTag tag) => tag._value;
    public static implicit operator PbfTag(uint tag) => new(tag);
}