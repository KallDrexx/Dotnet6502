using Dotnet6502.C64.Emulation;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// Emulates the NTSC VIC-II display system
/// </summary>
public class Vic2
{
    // Reference: https://zimmers.net/cbmpics/cbm/c64/vic-ii.txt
    private const int DotsPerCpuCycle = 8;
    private const int DotsPerScanline = 520;
    private const int VisibleDotsPerScanLine = 464;
    private const int TotalScanlines = 263;
    private const int FirstVisibleScanline = 16;
    private const int LastVisibleScanLine = 262;
    private const int FirstDisplayWindowScanLine = 51;
    private const int LastDisplayWindowScanLine = 250;
    private const int VisibleScanLines = LastVisibleScanLine - FirstVisibleScanline;

    private readonly IC64Display _c64Display;
    private readonly Memory<byte> _vic2Registers;
    private int _lineCycleCount;
    private int _lineDotCount;
    private int _currentScanLine;

    /// <summary>
    /// AKA VC, 10-bit counter that tracks which character/sprite data to fetch from memory. It acts as the main
    /// "position" counter for the current display line
    /// </summary>
    private ushort _videoCounter;

    /// <summary>
    /// AKA RC, 3-bit counter that tracks which scan line within a character row is currently being rendered
    /// </summary>
    private byte _rasterCounter;

    /// <summary>
    /// AKA VMLI, Serves as the base value for teh video counter. Stores the VC value at the beginning of each character row
    /// </summary>
    private ushort _videoMatrixLineIndex;

    public Vic2(IC64Display c64Display, IoMemoryArea ioMemoryArea)
    {
        _c64Display = c64Display;
        _vic2Registers = ioMemoryArea.Vic2Registers;
    }

    public void RunSingleCycle()
    {
        var vic2RegisterSpan = _vic2Registers.Span;

        _lineCycleCount++;
        _lineDotCount += DotsPerCpuCycle;
        if (_lineDotCount >= DotsPerCpuCycle)
        {
            AdvanceToNextScanLine(vic2RegisterSpan);
        }
    }

    private void AdvanceToNextScanLine(Span<byte> registers)
    {
        _currentScanLine++;
        _lineCycleCount = 0;
        _lineDotCount = 0;

        if (_currentScanLine > LastVisibleScanLine)
        {
            // Vblank starts
            _c64Display.RenderFrame(new RgbColor[VisibleDotsPerScanLine * VisibleScanLines]);
            _currentScanLine = 0; // vblank period comes before first visible scanlines
        }

        // Update the raster counter
        var scanLineMsbIsSet = (_currentScanLine & 0b1_0000_0000) > 0;
        registers[Vic2Registers.ControlRegister1] = scanLineMsbIsSet
            ? (byte)(registers[Vic2Registers.ControlRegister1] & 0b1111_1111)
            : (byte)(registers[Vic2Registers.ControlRegister1] & 0b0111_1111);

        registers[Vic2Registers.Raster] = (byte)(_currentScanLine & 0xFF);

        // Reset internal registers
        _videoCounter = 0;
        _videoMatrixLineIndex = 0;
        _rasterCounter = 0;
    }

    private void RunMemoryAccessPhase(Span<byte> registers)
    {
        if (_lineCycleCount < 10)
        {
            // TODO: Sprites (p-access for sprite pointers, s-access for sprite data)
        }

        else if (_lineCycleCount < 14)
        {
            // TODO: Idle
        }

        else if (_lineCycleCount < 54)
        {
            // TOOD: If badline, perform c-access (read screen memory for character codes)
            if (IsBadline(registers))
            {
                // TODO: implement
            }
        }

        else if (_lineCycleCount < 58)
        {
            // TODO: Graphics fetch - perform g-access (fetch character/bitmap data) using the character pointers
            // from c-access
        }

        else
        {
            // TODO: Refresh cycles, more sprite checks
        }
    }

    /// <summary>
    /// Determines if the current scanline is considered a badline. Badlines are lines where the Vic-II halts the
    /// CPU for 40 cycles in order to pull in new character and graphics data from memory.
    /// </summary>
    /// <returns></returns>
    private bool IsBadline(Span<byte> registers)
    {
        var displayEnable = (registers[Vic2Registers.ControlRegister1] & 0b0001_0000) > 0;
        var yScroll = registers[Vic2Registers.ControlRegister1] & 0b0000_0111;
        return displayEnable &&
               _currentScanLine >= 48 &&
               _currentScanLine <= 247 &&
               (_currentScanLine & 0b111) == yScroll;
    }
}