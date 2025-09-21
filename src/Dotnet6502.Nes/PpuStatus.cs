namespace Dotnet6502.Nes;

internal class PpuStatus
{
    public bool VBlankFlag { get; set; }
    public bool Sprite0HitFlag { get; set; }
    public bool SpriteOverflowFlag { get; set; }

    public byte ToByte()
    {
        return (byte)(
            (VBlankFlag ? 1 : 0) << 7 |
            (Sprite0HitFlag ? 1 : 0) << 6 |
            (SpriteOverflowFlag ? 1 : 0) << 5); // Leave open bus values as zero for now
    }
}