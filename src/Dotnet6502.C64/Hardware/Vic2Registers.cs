namespace Dotnet6502.C64.Hardware;

public static class Vic2Registers
{
    /// <summary>
    /// - 7   = Raster bit 8
    /// - 6   = ECM
    /// - 5   = BMM
    /// - 4   = Display Enable
    /// - 3   = RSEL
    /// - 2-0 = Yscroll
    /// </summary>
    public const ushort ControlRegister1 = 0x011;

    /// <summary>
    /// Raster bits 7-0
    /// </summary>
    public const ushort Raster = 0x012;

    /// <summary>
    /// - 7-6 = Unused
    /// - 5   = RES
    /// - 4   = MCM
    /// - 3   = CSEL
    /// - 2-0 = XScroll
    /// </summary>
    public const ushort ControlRegister2 = 0x016;

    /// <summary>
    /// - 7-4 = Screen pointer (A13-A10) (aka video screen matrix?)
    /// - 3-1 = Bitmapt/Charset pointer (A13-A11)
    /// - 0   = unused
    /// </summary>
    public const ushort VmCb = 0x018;
}