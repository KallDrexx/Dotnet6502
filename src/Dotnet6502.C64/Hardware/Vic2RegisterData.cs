namespace Dotnet6502.C64.Hardware;

public readonly ref struct Vic2RegisterData
{
    private readonly Span<byte> _registerBytes;

    public Vic2RegisterData(Span<byte> registerBytes)
    {
        _registerBytes = registerBytes;
    }

    /// <summary>
    /// Used for switching on/off the graphics
    /// </summary>
    public bool DisplayEnable => (_registerBytes[ControlRegister1] & (1 << 4)) > 0;

    public byte YScroll => (byte)(_registerBytes[ControlRegister1] & 0b111);

    public bool RSel => (_registerBytes[ControlRegister1] & (1 << 3)) > 0;

    public bool CSel => (_registerBytes[ControlRegister2] & (1 << 3)) > 0;

    public ushort RasterCounter
    {
        get
        {
            var result = (ushort)((_registerBytes[ControlRegister1] & (1 << 7)) << 8);
            result += _registerBytes[Raster];
            return result;
        }
        set
        {
            var msb = (value >> 8) & 1;
            var cr1 = _registerBytes[ControlRegister1];
            _registerBytes[ControlRegister1] = (byte)((cr1 & 0b0111_1111) + (msb << 7));
            _registerBytes[Raster] = (byte)value;
        }
    }

    public byte BorderColor => (byte)(_registerBytes[Ec] & 0xF);

    /// <summary>
    /// Pointer to the next area within the video matrix to load in
    /// </summary>
    public byte ScreenPointer => (byte)(_registerBytes[VmCb] >> 4);

    /// <summary>
    /// Points to the next area within the character map to load in
    /// </summary>
    public byte CharacterMapPointer => (byte)((_registerBytes[VmCb] & 0xF) >> 1);

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
    /// - 7-4 = Screen pointer (A13-A10) (aka video screen matrix?)
    /// - 3-1 = Bitmap/Charset pointer (A13-A11)
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
}