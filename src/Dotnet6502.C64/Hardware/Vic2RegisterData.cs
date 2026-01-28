using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class Vic2RegisterData
{
    private readonly IMemoryDevice _registerBytes;

    public Vic2RegisterData(IMemoryDevice registerBytes)
    {
        _registerBytes = registerBytes;
    }

    /// <summary>
    /// Used for switching on/off the graphics
    /// </summary>
    public bool DisplayEnable => (_registerBytes.Read(ControlRegister1) & (1 << 4)) > 0;

    public byte YScroll => (byte)(_registerBytes.Read(ControlRegister1) & 0b111);

    public bool RSel => (_registerBytes.Read(ControlRegister1) & (1 << 3)) > 0;

    public bool CSel => (_registerBytes.Read(ControlRegister2) & (1 << 3)) > 0;

    public ushort RasterCounter
    {
        get
        {
            var result = (ushort)((_registerBytes.Read(ControlRegister1) & (1 << 7)) << 8);
            result += _registerBytes.Read(Raster);
            return result;
        }
        set
        {
            var msb = (value >> 8) & 1;
            var cr1 = _registerBytes.Read(ControlRegister1);
            _registerBytes.Write(ControlRegister1, (byte)((cr1 & 0b0111_1111) + (msb << 7)));
            _registerBytes.Write(Raster, (byte)value);
        }
    }

    public byte BorderColor => (byte)(_registerBytes.Read(Ec) & 0xF);

    public byte BackgroundColor0 => (byte)(_registerBytes.Read(B0C) & 0xF);

    public byte BackgroundColor1 => (byte)(_registerBytes.Read(B1C) & 0xF);

    public byte BackgroundColor2 => (byte)(_registerBytes.Read(B2C) & 0xF);

    public byte BackgroundColor3 => (byte)(_registerBytes.Read(B3C) & 0xF);

    public byte XScroll => (byte)(_registerBytes.Read(ControlRegister2) & 0b111);

    /// <summary>
    /// Pointer to the next area within the video matrix to load in
    /// </summary>
    public byte ScreenPointer => (byte)(_registerBytes.Read(VmCb) >> 4);

    /// <summary>
    /// Points to the next area within the character map to load in
    /// </summary>
    public byte CharacterMapPointer => (byte)((_registerBytes.Read(VmCb) & 0xF) >> 1);

    public bool Ecm => (_registerBytes.Read(ControlRegister1) & 0b0100_0000) > 0;

    public bool Bmm => (_registerBytes.Read(ControlRegister1) & 0b0010_0000) > 0;

    public bool Mcm => (_registerBytes.Read(ControlRegister2) & 0b00010000) > 0;

    /// <summary>
    /// - 7   = Raster bit 8
    /// - 6   = ECM
    /// - 5   = BMM
    /// - 4   = Display Enable - used for switching on/off the graphics.
    /// - 3   = RSEL
    /// - 2-0 = Yscroll
    /// </summary>
    private const ushort ControlRegister1 = 0x011;

    /// <summary>
    /// Raster bits 7-0
    /// </summary>
    private const ushort Raster = 0x012;

    /// <summary>
    /// - 7-6 = Unused
    /// - 5   = RES
    /// - 4   = MCM
    /// - 3   = CSEL
    /// - 2-0 = XScroll
    /// </summary>
    private const ushort ControlRegister2 = 0x016;

    /// <summary>
    /// - 7-4 = Screen pointer (A13-A10) (aka video screen matrix?) (VM13-VM10)
    /// - 3-1 = Bitmap/Charset pointer (A13-A11) / (CB13-CB11)
    /// - 0   = unused
    /// </summary>
    private const ushort VmCb = 0x018;

    /// <summary>
    /// - 7 = IRQ - Represents the inverted state of the IRQ output
    /// - 3 = ILP - Interrupt due to negative edge of the LP input (lightpen)
    /// - 2 = IMMC - Interrupt due to collision of two or more sprites
    /// - 1 = IMBC - Interrupt due to collision of at least one sprite with text/bitmap data
    /// - 0 = IRST - Interrupt due to reaching the interrupt raster line
    /// </summary>
    private const ushort Irqst = 0x019;

    /// <summary>
    /// Border color (bits 3-0)
    /// </summary>
    private const ushort Ec = 0x020;

    /// <summary>
    /// Background color 0 (bits 3-0)
    /// </summary>
    private const ushort B0C = 0x021;

    /// <summary>
    /// Background color 1 (bits 3-0)
    /// </summary>
    private const ushort B1C = 0x022;

    /// <summary>
    /// Background color 2 (bits 3-0)
    /// </summary>
    private const ushort B2C = 0x023;

    /// <summary>
    /// Background color 3 (bits 3-0)
    /// </summary>
    private const ushort B3C = 0x024;

    // Sprite register offsets
    private const ushort SpriteXBase = 0x000;      // $D000-$D00E (even bytes)
    private const ushort SpriteYBase = 0x001;      // $D001-$D00F (odd bytes)
    private const ushort SpriteXMsb = 0x010;       // $D010 - MSB of X coordinates
    private const ushort SpriteEnable = 0x015;     // $D015 - Sprite enable
    private const ushort SpriteYExpand = 0x017;    // $D017 - Y expansion
    private const ushort SpritePriority = 0x01B;   // $D01B - Priority (0=front, 1=behind)
    private const ushort SpriteMulticolor = 0x01C; // $D01C - Multicolor mode enable
    private const ushort SpriteXExpand = 0x01D;    // $D01D - X expansion
    private const ushort SpriteSpriteCollision = 0x01E;  // $D01E - Sprite-sprite collision (read-only)
    private const ushort SpriteDataCollision = 0x01F;    // $D01F - Sprite-data collision (read-only)
    private const ushort SpriteMulticolor0 = 0x025;      // $D025 - Multicolor color 0 (shared)
    private const ushort SpriteMulticolor1 = 0x026;      // $D026 - Multicolor color 1 (shared)
    private const ushort SpriteColorBase = 0x027;        // $D027-$D02E - Individual sprite colors

    /// <summary>
    /// Gets the X position for a sprite (full 9-bit value including MSB)
    /// </summary>
    public ushort GetSpriteX(int spriteIndex)
    {
        var lowByte = _registerBytes.Read((ushort)(SpriteXBase + spriteIndex * 2));
        var msb = (_registerBytes.Read(SpriteXMsb) >> spriteIndex) & 1;
        return (ushort)(lowByte | (msb << 8));
    }

    /// <summary>
    /// Gets the Y position for a sprite
    /// </summary>
    public byte GetSpriteY(int spriteIndex)
    {
        return _registerBytes.Read((ushort)(SpriteYBase + spriteIndex * 2));
    }

    /// <summary>
    /// Gets whether a sprite is enabled
    /// </summary>
    public bool IsSpriteEnabled(int spriteIndex)
    {
        return ((_registerBytes.Read(SpriteEnable) >> spriteIndex) & 1) == 1;
    }

    /// <summary>
    /// Gets whether a sprite has Y expansion (double height)
    /// </summary>
    public bool IsSpriteYExpanded(int spriteIndex)
    {
        return ((_registerBytes.Read(SpriteYExpand) >> spriteIndex) & 1) == 1;
    }

    /// <summary>
    /// Gets whether a sprite has X expansion (double width)
    /// </summary>
    public bool IsSpriteXExpanded(int spriteIndex)
    {
        return ((_registerBytes.Read(SpriteXExpand) >> spriteIndex) & 1) == 1;
    }

    /// <summary>
    /// Gets whether a sprite is behind graphics data (1=behind, 0=in front)
    /// </summary>
    public bool IsSpriteBehindData(int spriteIndex)
    {
        return ((_registerBytes.Read(SpritePriority) >> spriteIndex) & 1) == 1;
    }

    /// <summary>
    /// Gets whether a sprite is in multicolor mode
    /// </summary>
    public bool IsSpriteMulticolor(int spriteIndex)
    {
        return ((_registerBytes.Read(SpriteMulticolor) >> spriteIndex) & 1) == 1;
    }

    /// <summary>
    /// Gets the individual color for a sprite (bits 3-0)
    /// </summary>
    public byte GetSpriteColor(int spriteIndex)
    {
        return (byte)(_registerBytes.Read((ushort)(SpriteColorBase + spriteIndex)) & 0x0F);
    }

    /// <summary>
    /// Gets sprite multicolor 0 (shared by all sprites)
    /// </summary>
    public byte SpriteMulticolorColor0 => (byte)(_registerBytes.Read(SpriteMulticolor0) & 0x0F);

    /// <summary>
    /// Gets sprite multicolor 1 (shared by all sprites)
    /// </summary>
    public byte SpriteMulticolorColor1 => (byte)(_registerBytes.Read(SpriteMulticolor1) & 0x0F);
}