using Dotnet6502.C64.Emulation;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// Emulates the NTSC VIC-II display system, based on the 6567R8 NTSC-M
/// </summary>
public class Vic2
{
    // Reference: https://zimmers.net/cbmpics/cbm/c64/vic-ii.txt
    public enum GraphicsMode
    {
        StandardTextMode,
        MulticolorTextMode,
        StandardBitmapMode,
        MulticolorBitmapMode,
        EcmTextMode,
        Invalid,
    }

    private const int DotsPerCpuCycle = 8;
    private const int DotsPerScanline = 520;
    public const int VisibleDotsPerScanLine = 418;
    private const int TotalScanlines = 263;
    private const int FirstVisibleScanline = 28;
    private const int LastVisibleScanLine = 262;
    private const int FirstDisplayWindowScanLine = 51;
    private const int LastDisplayWindowScanLine = 250;
    public const int VisibleScanLines = LastVisibleScanLine - FirstVisibleScanline + 1;

    private readonly IC64Display _c64Display;
    private readonly Memory<byte> _vic2Registers;
    // private readonly Memory<byte> _colorRam;
    private readonly ReadOnlyMemory<byte> _screenRam;
    private readonly RgbColor[] _frameBuffer = new RgbColor[VisibleDotsPerScanLine * VisibleScanLines];
    private readonly RgbColor[] _palette = new RgbColor[16];
    private int _lineCycleCount;
    private int _lineDotCount;
    private ushort _currentScanLine;

    /// <summary>
    /// AKA VC, 10-bit counter that tracks which character/sprite data to fetch from memory. It acts as the main
    /// "position" counter for the current display line
    /// </summary>
    private ushort _videoCounter;

    /// <summary>
    /// AKA VCBASE, 10-bit dta register with reset input that can be loaded with the value from VC
    /// </summary>
    private ushort _videoCounterBase;

    /// <summary>
    /// AKA RC, 3-bit counter that tracks which scan line within a character row is currently being rendered
    /// </summary>
    private byte _rowCounter;

    /// <summary>
    /// AKA VMLI, 6-bit counter with reset input that keeps track of the position within the internal
    /// 40x12 bit video matrix/color line where read character pointers are stored.
    /// </summary>
    private ushort _videoMatrixLineIndex;

    public Vic2(IC64Display c64Display, IoMemoryArea ioMemoryArea, ReadOnlyMemory<byte> screenRam)
    {
        _c64Display = c64Display;
        _screenRam = screenRam;
        _vic2Registers = ioMemoryArea.Vic2Registers;
        // _colorRam = ioMemoryArea.ColorRam;

        // Fill in the palette values based on the Colodore Palette
        _palette[0] = new RgbColor(0, 0, 0);       // Black
        _palette[1] = new RgbColor(255, 255, 255); // White
        _palette[2] = new RgbColor(129, 51, 56);   // Red
        _palette[3] = new RgbColor(117, 206, 200); // Cyan
        _palette[4] = new RgbColor(142, 60, 151);  // Purple
        _palette[5] = new RgbColor(86, 172, 77);   // Green
        _palette[6] = new RgbColor(46, 44, 155);   // Blue
        _palette[7] = new RgbColor(237, 241, 113); // Yellow
        _palette[8] = new RgbColor(108, 85, 36);   // Orange
        _palette[9] = new RgbColor(92, 71, 0);     // Brown
        _palette[10] = new RgbColor(180, 105, 98); // Light Red
        _palette[11] = new RgbColor(95, 95, 95);   // Dark Gray
        _palette[12] = new RgbColor(137, 137, 137); // Gray
        _palette[13] = new RgbColor(154, 226, 155); // Light Green
        _palette[14] = new RgbColor(136, 126, 203); // Light Blue
        _palette[15] = new RgbColor(173, 173, 173); // Light Gray
    }

    public void RunSingleCycle()
    {
        var registers = new Vic2RegisterData(_vic2Registers.Span);

        _lineCycleCount++;
        _lineDotCount += DotsPerCpuCycle;
        if (_lineDotCount >= DotsPerCpuCycle)
        {
            AdvanceToNextScanLine(registers);
        }

        RunMemoryAccessPhase(registers);
        UpdateFramebuffer(registers);
    }

    private void AdvanceToNextScanLine(Vic2RegisterData registers)
    {
        _currentScanLine++;
        _lineCycleCount = 0;
        _lineDotCount = 0;

        if (_currentScanLine > LastVisibleScanLine)
        {
            // Vblank starts
            _c64Display.RenderFrame(_frameBuffer);
            _currentScanLine = 0; // vblank period is the beginning of the scanlines

            for (var x = 0; x < _frameBuffer.Length; x++)
            {
                _frameBuffer[x] = new RgbColor(0, 0, 0);
            }
        }

        // Update the raster counter
        registers.RasterCounter = _currentScanLine;

        // Reset internal registers
        _videoCounter = 0;
        _videoMatrixLineIndex = 0;
        _rowCounter = 0;
    }

    private void RunMemoryAccessPhase(Vic2RegisterData registerData)
    {
        if (_lineCycleCount < 10)
        {
            // TODO: Sprites (p-access for sprite pointers, s-access for sprite data)
        }

        else if (_lineCycleCount < 14)
        {
            // TODO: Idle
        }

        else if (_lineCycleCount == 14)
        {
            _videoCounter = _videoCounterBase;
            _videoMatrixLineIndex = 0;

            if (IsBadline(registerData))
            {
                _rowCounter = 0;

                // TODO: trigger badline condition
            }
        }

        else if (_lineCycleCount < 54)
        {
            // Perform C-access read if we are in a badline
        }

        else if (_lineCycleCount < 58)
        {
            // TODO: Graphics fetch - perform g-access (fetch character/bitmap data) using the character pointers
            // from c-access
        }

        else if (_lineCycleCount == 58)
        {
            if (_rowCounter == 7)
            {

            }
        }

        else
        {
            // TODO: Refresh cycles, more sprite checks
        }
    }

    private void UpdateFramebuffer(Vic2RegisterData registerData)
    {
        if (_currentScanLine < FirstVisibleScanline ||
            _currentScanLine > LastVisibleScanLine || // just to be safe
            _lineDotCount >= VisibleDotsPerScanLine)
        {
            // Not in a visible area
            return;
        }

        var csel = registerData.CSel;
        var rsel = registerData.RSel;
        var lastBorderLeftDot = csel ? 24 : 31;
        var firstBorderRightDot = csel ? 344 : 335;
        var lastBorderTopLine = rsel ? 51 : 55;
        var firstBorderBottomLine = rsel ? 247 : 251;
        var borderColor = registerData.BorderColor;

        // var colorRam = _colorRam.Span;
        var screenRam = _screenRam.Span;

        var row = (_currentScanLine - FirstVisibleScanline) / 8;
        var rowInChar = (_currentScanLine - FirstVisibleScanline) % 8;
        var column = _lineCycleCount - 17;

        // Write the next 8 dots
        for (var x = 0; x < DotsPerCpuCycle; x++)
        {
            var dot = _lineDotCount + x;
            var pixelIndex = (_currentScanLine - FirstVisibleScanline) * VisibleDotsPerScanLine + dot;
            if (_currentScanLine <= lastBorderTopLine ||
                _currentScanLine >= firstBorderBottomLine ||
                dot <= lastBorderLeftDot ||
                dot >= firstBorderRightDot)
            {
                _frameBuffer[pixelIndex] = _palette[borderColor];
                continue;
            }

            // Standard text only mode atm
            var columnInChar = x;
            var charCode = screenRam[row * 40 + column];
            // var color = colorRam[row * 40 + column];
        }
    }

    /// <summary>
    /// Determines if the current scanline is considered a badline. Badlines are lines where the Vic-II halts the
    /// CPU for 40 cycles in order to pull in new character and graphics data from memory.
    /// </summary>
    /// <returns></returns>
    private bool IsBadline(Vic2RegisterData registerData)
    {
        return registerData.DisplayEnable &&
               _currentScanLine >= 48 &&
               _currentScanLine <= 247 &&
               (_currentScanLine & 0b111) == registerData.YScroll;
    }

    /// <summary>
    /// Gets the current graphics mode based on the current register set
    /// </summary>
    /// <param name="registerData"></param>
    /// <returns></returns>
    private GraphicsMode GetGraphicsMode(Vic2RegisterData registerData)
    {
        var ecm = registerData.Ecm;
        var bmm = registerData.Bmm;
        var mcm = registerData.Mcm;

        return (ecm, bmm, mcm) switch
        {
            (false, false, false) => GraphicsMode.StandardTextMode,
            (false, false, true) => GraphicsMode.MulticolorTextMode,
            (false, true, false) => GraphicsMode.StandardBitmapMode,
            (false, true, true) => GraphicsMode.MulticolorBitmapMode,
            (true, false, false) => GraphicsMode.EcmTextMode,
            _ => GraphicsMode.Invalid,
        };
    }
}