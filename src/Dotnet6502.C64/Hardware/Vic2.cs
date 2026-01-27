using Dotnet6502.C64.Emulation;
using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// Emulates the NTSC VIC-II display system, based on the 6567R8 NTSC-M
/// </summary>
public class Vic2
{
    // Reference: https://zimmers.net/cbmpics/cbm/c64/vic-ii.txt
    private enum GraphicsMode
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
    private readonly Vic2RegisterData _vic2Registers;
    private readonly MemoryBus _ramView;
    private readonly ComplexInterfaceAdapter _cia2;
    private readonly BasicRamMemoryDevice _colorRam;
    private readonly RgbColor[] _frameBuffer = new RgbColor[VisibleDotsPerScanLine * VisibleScanLines];
    private readonly RgbColor[] _palette = new RgbColor[16];
    private int _lineCycleCount;
    private int _lineDotCount;
    private ushort _currentScanLine;
    private bool _inDisplayState; // When false, in idle state
    private bool _inBadline;

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

    /// <summary>
    /// 40 entries of 12 bits. Bits 0-7 contain video matrix data (character codes) while
    /// bits 8-11 contain color information.
    /// </summary>
    private ushort[] _videoColorBuffer = new ushort[40];

    /// <summary>
    /// Stores the 8-pixel pattern data from the most recent g-access.
    /// </summary>
    private byte[] _graphicsDataBuffer = new byte[40];

    /// <summary>
    /// Determines if the VIC triggers an IRQ. Will be set to false once the value is read.
    /// </summary>
    public bool IrqTriggered
    {
        get
        {
            var value = field;
            field = false;
            return value;
        }
        private set;
    }

    public Vic2(IC64Display c64Display, C64MemoryConfig memoryConfig)
    {
        _c64Display = c64Display;
        _vic2Registers = new Vic2RegisterData(memoryConfig.IoMemoryArea.Vic2Registers);
        _cia2 = memoryConfig.IoMemoryArea.Cia2;
        _ramView = memoryConfig.Vic2MemoryBus;
        _colorRam = memoryConfig.IoMemoryArea.ColorRam;

        // Colodore palette - https://www.colodore.com/
        _palette[0] = new RgbColor(0, 0, 0);        // Black
        _palette[1] = new RgbColor(255, 255, 255);  // White
        _palette[2] = new RgbColor(129, 51, 56);    // Red
        _palette[3] = new RgbColor(117, 206, 200);  // Cyan
        _palette[4] = new RgbColor(142, 60, 151);   // Purple
        _palette[5] = new RgbColor(86, 172, 77);    // Green
        _palette[6] = new RgbColor(46, 44, 155);    // Blue
        _palette[7] = new RgbColor(237, 241, 113);  // Yellow
        _palette[8] = new RgbColor(142, 80, 41);    // Orange
        _palette[9] = new RgbColor(85, 56, 0);      // Brown
        _palette[10] = new RgbColor(196, 108, 113); // Light Red
        _palette[11] = new RgbColor(74, 74, 74);    // Dark Gray
        _palette[12] = new RgbColor(123, 123, 123); // Gray
        _palette[13] = new RgbColor(169, 255, 159); // Light Green
        _palette[14] = new RgbColor(112, 109, 235); // Light Blue
        _palette[15] = new RgbColor(178, 178, 178); // Light Gray
    }

    /// <summary>
    /// Runs a single VIC-II cycle
    /// </summary>
    /// <returns>
    /// True if a bad line condition has been meant. This indicates that the hardware should run
    /// 40 cycles before executing the next CPU instruction.
    /// </returns>
    public bool RunSingleCycle()
    {
        _lineCycleCount++;
        _lineDotCount += DotsPerCpuCycle;
        if (_lineDotCount >= DotsPerScanline)
        {
            AdvanceToNextScanLine();
        }

        var startsBadLine = RunMemoryAccessPhase();
        UpdateFramebuffer();

        return startsBadLine;
    }

    private void AdvanceToNextScanLine()
    {
        _currentScanLine++;
        _lineCycleCount = 0;
        _lineDotCount = 0;

        if (_currentScanLine > LastVisibleScanLine)
        {
            // Vblank starts
            _c64Display.RenderFrame(_frameBuffer);
            _currentScanLine = 0; // vblank period is the beginning of the scanlines
            IrqTriggered = true;

            for (var x = 0; x < _frameBuffer.Length; x++)
            {
                _frameBuffer[x] = new RgbColor(0, 0, 0);
            }

            // Reset VCBASE at frame start only
            _videoCounterBase = 0;
        }

        // Update the raster counter
        _vic2Registers.RasterCounter = _currentScanLine;
    }

    private bool RunMemoryAccessPhase()
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

            if (IsBadline())
            {
                _rowCounter = 0;
                _inDisplayState = true;
                _inBadline = true;

                return true;
            }
        }

        else if (_lineCycleCount >= 15 && _lineCycleCount <= 54)
        {
            // C-access: fetch character code and color on badlines
            _inBadline = IsBadline();
            if (_inBadline)
            {
                var ramAddress = _videoCounter;
                ramAddress += (ushort)(_vic2Registers.ScreenPointer << 10);
                var charCode = ReadRam(ramAddress);
                var colorByte = _colorRam.Read(_videoCounter);

                // Store 12-bit value: bits 0-7 = char code, bits 8-11 = color
                _videoColorBuffer[_videoMatrixLineIndex] = (ushort)(charCode | ((colorByte & 0x0F) << 8));
            }

            // G-access: fetch pixel pattern from character generator
            if (_inDisplayState)
            {
                var charCode = (byte)(_videoColorBuffer[_videoMatrixLineIndex] & 0xFF);
                var charRomAddress = (ushort)((charCode * 8) + _rowCounter);
                charRomAddress += (ushort)(_vic2Registers.CharacterMapPointer << 11);

                _graphicsDataBuffer[_videoMatrixLineIndex] = ReadRam(charRomAddress);

                _videoCounter++;
                _videoMatrixLineIndex++;
            }
        }

        else if (_lineCycleCount == 58)
        {
            if (_rowCounter == 7)
            {
                // Store VC into VCBASE at end of character row
                _videoCounterBase = _videoCounter;

                if (!_inBadline)
                {
                    _inDisplayState = false;
                }
            }
            else if (_inDisplayState)
            {
                // Increment row counter within character
                _rowCounter++;
            }
        }

        else
        {
            // TODO: Refresh cycles, more sprite checks
        }

        return false;
    }

    private void UpdateFramebuffer()
    {
        if (_currentScanLine < FirstVisibleScanline ||
            _currentScanLine > LastVisibleScanLine || // just to be safe
            _lineDotCount >= VisibleDotsPerScanLine)
        {
            // Not in a visible area
            return;
        }

        var csel = _vic2Registers.CSel;
        var rsel = _vic2Registers.RSel;
        var lastBorderLeftDot = csel ? 46 : 53;
        var firstBorderRightDot = csel ? 367 : 358;
        var lastBorderTopLine = rsel ? 51 : 55;
        var firstBorderBottomLine = rsel ? 251 : 247;
        var borderColor = _vic2Registers.BorderColor;

        // Write the next 8 dots
        for (var x = 0; x < DotsPerCpuCycle; x++)
        {
            var dot = _lineDotCount + x;
            if (dot >= VisibleDotsPerScanLine)
            {
                break;
            }

            var pixelIndex = (_currentScanLine - FirstVisibleScanline) * VisibleDotsPerScanLine + dot;
            if (_currentScanLine <= lastBorderTopLine ||
                _currentScanLine >= firstBorderBottomLine ||
                dot <= lastBorderLeftDot ||
                dot >= firstBorderRightDot)
            {
                _frameBuffer[pixelIndex] = _palette[borderColor];
                continue;
            }

            // Render standard text mode pixel
            var displayX = dot - (csel ? 47 : 54);
            var charColumn = displayX / 8;
            var bitPosition = 7 - (displayX % 8);

            if (charColumn >= 0 && charColumn < 40)
            {
                var pixelData = _graphicsDataBuffer[charColumn];
                var isForeground = ((pixelData >> bitPosition) & 1) == 1;

                if (isForeground)
                {
                    var fgColor = (_videoColorBuffer[charColumn] >> 8) & 0x0F;
                    _frameBuffer[pixelIndex] = _palette[fgColor];
                }
                else
                {
                    _frameBuffer[pixelIndex] = _palette[_vic2Registers.BackgroundColor0];
                }
            }
            else
            {
                _frameBuffer[pixelIndex] = _palette[_vic2Registers.BackgroundColor0];
            }
        }
    }

    /// <summary>
    /// Determines if the current scanline is considered a badline. Badlines are lines where the Vic-II halts the
    /// CPU for 40 cycles in order to pull in new character and graphics data from memory.
    /// </summary>
    /// <returns></returns>
    private bool IsBadline()
    {
        return _vic2Registers.DisplayEnable &&
               _currentScanLine >= 48 &&
               _currentScanLine <= 247 &&
               (_currentScanLine & 0b111) == _vic2Registers.YScroll;
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

    private byte ReadRam(ushort address)
    {
        // The VIC only knows about the first 14 bits of the address. The 2 MSB are set by CIA2 to determine
        // what bank the VIC ends up reading. The CIA2 has the two bits inverted, so 0b01 translates to
        // 0b10 in the address call.
        var bank = (ushort)(((_cia2.DataPortA & 0b11) ^ 0b11) << 14);
        address = (ushort)(bank | (address & 0b0011_1111_1111_1111));

        return _ramView.Read(address);
    }
}