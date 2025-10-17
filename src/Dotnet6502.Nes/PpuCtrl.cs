namespace Dotnet6502.Nes;

internal class PpuCtrl
{
    private byte _rawByte;
    public NmiEnableValue NmiEnable { get; set; }
    public MasterSlaveSelectValue MasterSlaveSelect { get; set; }
    public SpriteSizeValue SpriteSize { get; set; }
    public BackgroundPatternTableAddressEnum BackgroundPatternTableAddress { get; set; }
    public SpritePatternTableAddressFor8X8Enum SpritePatternTableAddressFor8X8 { get; set; }
    public VRamAddressIncrementValue VRamAddressIncrement { get; set; }
    public BaseNameTableAddressValue BaseNameTableAddress { get; set; }

    public byte RawByte
    {
        get => _rawByte;
        set => UpdateFromByte(value);
    }

    public void UpdateFromByte(byte value)
    {
        NmiEnable = (NmiEnableValue)((value & 0x80) >> 7);
        MasterSlaveSelect = (MasterSlaveSelectValue)((value & 0x40) >> 6);
        SpriteSize = (SpriteSizeValue)((value & 0x20) >> 5);
        BackgroundPatternTableAddress = (BackgroundPatternTableAddressEnum)((value & 0x10) >> 4);
        SpritePatternTableAddressFor8X8 = (SpritePatternTableAddressFor8X8Enum)((value & 0x08) >> 3);
        VRamAddressIncrement = (VRamAddressIncrementValue)((value & 0x04) >> 2);
        BaseNameTableAddress = (BaseNameTableAddressValue)(value & 0x03);

        _rawByte = value;
    }

    internal byte ToByte()
    {
        return (byte)(
            (byte)NmiEnable << 7 |
            (byte)MasterSlaveSelect << 6 |
            (byte)SpriteSize << 5 |
            (byte)BackgroundPatternTableAddress << 4 |
            (byte)SpritePatternTableAddressFor8X8 << 3 |
            (byte)VRamAddressIncrement << 2 |
            (byte)BaseNameTableAddress);
    }

    internal enum BackgroundPatternTableAddressEnum
    {
        Hex0000 = 0,
        Hex1000 = 1,
    }

    internal enum SpritePatternTableAddressFor8X8Enum
    {
        Hex0000 = 0,
        Hex1000 = 1,
    }

    public enum MasterSlaveSelectValue
    {
        ReadBackdropFromExtPins = 0,
        WriteColorOnExtPins = 1
    }

    public enum SpriteSizeValue
    {
        EightByEight = 0,
        EightBySixteen = 1,
    }

    public enum BaseNameTableAddressValue
    {
        Hex2000 = 0,
        Hex2400 = 1,
        Hex2800 = 2,
        Hex2C00 = 3,
    }

    internal enum NmiEnableValue
    {
        Off = 0,
        On = 1
    }

    internal enum VRamAddressIncrementValue
    {
        Add1Across = 0,
        Add32Down = 1,
    }
}