namespace Dotnet6502.Nes;

internal class PpuMask
{
    public bool EmphasizeBlue { get; set; }
    public bool EmphasizeGreen { get; set; }
    public bool EmphasizeRed { get; set; }
    public bool EnableSpriteRendering { get; set; }
    public bool EnableBackgroundRendering { get; set; }
    public bool ShowSpritesInLeftmost8PixelsOfScreen { get; set; }
    public bool ShowBackgroundInLeftmost8PixelsOfScreen { get; set; }
    public bool Greyscale { get; set; }

    public void UpdateFromByte(byte value)
    {
        EmphasizeBlue = (value & 0x80) > 0;
        EmphasizeGreen = (value & 0x40) > 0;
        EmphasizeRed = (value & 0x20) > 0;
        EnableSpriteRendering = (value & 0x10) > 0;
        EnableBackgroundRendering = (value & 0x08) > 0;
        ShowSpritesInLeftmost8PixelsOfScreen = (value & 0x04) > 0;
        ShowBackgroundInLeftmost8PixelsOfScreen = (value & 0x02) > 0;
        Greyscale = (value & 0x01) > 0;
    }
}
