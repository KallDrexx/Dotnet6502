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
    private readonly Vic2MemoryDevice _vic2RegisterDevice;
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

    // Sprite state
    private readonly byte[] _spritePointers = new byte[8];       // Fetched during P-access
    private readonly byte[,] _spriteDataBuffer = new byte[8, 3]; // 3 bytes per sprite line
    private readonly uint[] _spriteShiftRegisters = new uint[8]; // 24-bit shift registers
    private readonly byte[] _spriteCurrentLine = new byte[8];    // Current line within sprite (0-20)
    private readonly bool[] _spriteDmaEnabled = new bool[8];     // DMA active for sprite
    private readonly bool[] _spriteDisplayEnabled = new bool[8]; // Sprite currently displaying
    private readonly bool[] _spriteYExpandFlipFlop = new bool[8]; // For Y expansion timing
    private byte _activeSpriteMask;                              // Bitmask of sprites currently displaying

    // Sprite rendering scratch buffers (pre-allocated to avoid per-pixel allocations)
    private readonly int[] _spritePixelColors = new int[8];      // -1 = transparent, 0-15 = color index

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
        _vic2RegisterDevice = memoryConfig.IoMemoryArea.Vic2Registers;
        _vic2Registers = new Vic2RegisterData(_vic2RegisterDevice);
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
        // Advance sprite state before moving to next line
        AdvanceSpriteState();

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
        // Sprite handling runs in parallel with other VIC operations
        RunSpriteMemoryAccess();

        // Main graphics memory access phase
        if (_lineCycleCount < 14)
        {
            // Idle cycles
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

            // G-access: fetch pixel pattern from character generator or bitmap memory
            if (_inDisplayState)
            {
                ushort dataAddress;
                var mode = GetGraphicsMode(_vic2Registers);

                if (mode == GraphicsMode.StandardBitmapMode || mode == GraphicsMode.MulticolorBitmapMode)
                {
                    // Bitmap modes: read from bitmap memory
                    // Bitmap base is determined by bit 3 of CharacterMapPointer (CB13)
                    var bitmapBase = (ushort)((_vic2Registers.CharacterMapPointer & 0x04) << 11);
                    dataAddress = (ushort)(bitmapBase + (_videoCounter * 8) + _rowCounter);
                }
                else
                {
                    // Text modes: read from character ROM/RAM
                    var charCode = (byte)(_videoColorBuffer[_videoMatrixLineIndex] & 0xFF);

                    // In ECM mode, only lower 6 bits of character code are used
                    if (mode == GraphicsMode.EcmTextMode)
                    {
                        charCode = (byte)(charCode & 0x3F);
                    }

                    dataAddress = (ushort)((charCode * 8) + _rowCounter);
                    dataAddress += (ushort)(_vic2Registers.CharacterMapPointer << 11);
                }

                _graphicsDataBuffer[_videoMatrixLineIndex] = ReadRam(dataAddress);

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
        var xScroll = _vic2Registers.XScroll;
        var lastBorderLeftDot = csel ? 46 : 53;
        var firstBorderRightDot = csel ? 367 : 358;
        var lastBorderTopLine = rsel ? 51 : 55;
        var firstBorderBottomLine = rsel ? 251 : 247;
        var borderColor = _vic2Registers.BorderColor;
        var mode = GetGraphicsMode(_vic2Registers);

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

            // Calculate display position with X-scroll
            var displayX = dot - (csel ? 47 : 54) + xScroll;

            // Render graphics and get foreground status
            var (graphicsColor, isForeground) = mode switch
            {
                GraphicsMode.StandardTextMode => RenderStandardTextModePixelWithFg(displayX),
                GraphicsMode.MulticolorTextMode => RenderMulticolorTextModePixelWithFg(displayX),
                GraphicsMode.StandardBitmapMode => RenderStandardBitmapModePixelWithFg(displayX),
                GraphicsMode.MulticolorBitmapMode => RenderMulticolorBitmapModePixelWithFg(displayX),
                GraphicsMode.EcmTextMode => RenderEcmTextModePixelWithFg(displayX),
                GraphicsMode.Invalid => (_palette[0], false),
                _ => (_palette[_vic2Registers.BackgroundColor0], false),
            };

            // Render sprites and composite with graphics
            var finalColor = RenderSpritesAtPosition(dot, graphicsColor, isForeground);

            _frameBuffer[pixelIndex] = finalColor;
        }
    }

    /// <summary>
    /// Renders sprites at the given screen position and composites with graphics
    /// </summary>
    private RgbColor RenderSpritesAtPosition(int screenX, RgbColor graphicsColor, bool graphicsIsForeground)
    {
        // Fast path: no sprites are currently displaying
        if (_activeSpriteMask == 0)
        {
            return graphicsColor;
        }

        // Use a bitmask to track which sprites have visible pixels at this position
        byte spritesAtPositionMask = 0;

        // Check each sprite (lower index = higher priority)
        for (var i = 0; i < 8; i++)
        {
            _spritePixelColors[i] = -1; // Default to transparent

            if (!_spriteDisplayEnabled[i])
            {
                continue;
            }

            var spriteX = _vic2Registers.GetSpriteX(i);
            var isXExpanded = _vic2Registers.IsSpriteXExpanded(i);
            var spriteWidth = isXExpanded ? 48 : 24;

            // Sprite X position is offset by 24 pixels from screen coordinates
            var spriteScreenX = spriteX - 24;

            // Check if current pixel is within sprite bounds
            if (screenX < spriteScreenX || screenX >= spriteScreenX + spriteWidth)
            {
                continue;
            }

            // Calculate pixel position within sprite
            var pixelInSprite = screenX - spriteScreenX;
            if (isXExpanded)
            {
                pixelInSprite /= 2; // Each pixel is doubled in X expansion
            }

            // Get pixel from shift register
            var colorIndex = GetSpritePixelColor(i, pixelInSprite);
            _spritePixelColors[i] = colorIndex;

            if (colorIndex >= 0)
            {
                spritesAtPositionMask |= (byte)(1 << i);
            }
        }

        // Early exit if no sprites at this position
        if (spritesAtPositionMask == 0)
        {
            return graphicsColor;
        }

        // Count sprites for collision detection
        var spriteCount = System.Numerics.BitOperations.PopCount(spritesAtPositionMask);

        // Collision detection: sprite-sprite (2+ sprites overlapping)
        if (spriteCount >= 2)
        {
            for (var i = 0; i < 8; i++)
            {
                if ((spritesAtPositionMask & (1 << i)) != 0)
                {
                    _vic2RegisterDevice.SetSpriteSpriteCollision(i);
                }
            }
        }

        // Collision detection: sprite-data
        if (graphicsIsForeground)
        {
            for (var i = 0; i < 8; i++)
            {
                if ((spritesAtPositionMask & (1 << i)) != 0)
                {
                    _vic2RegisterDevice.SetSpriteDataCollision(i);
                }
            }
        }

        // Determine final color based on priority
        // Find highest priority (lowest index) visible sprite
        for (var i = 0; i < 8; i++)
        {
            if ((spritesAtPositionMask & (1 << i)) == 0)
            {
                continue;
            }

            var colorIndex = _spritePixelColors[i];
            var isBehindData = _vic2Registers.IsSpriteBehindData(i);

            if (isBehindData && graphicsIsForeground)
            {
                // Sprite is behind graphics, graphics wins
                continue;
            }

            // Sprite is visible
            return _palette[colorIndex];
        }

        return graphicsColor;
    }

    /// <summary>
    /// Gets the color index for a sprite pixel, or -1 if transparent
    /// </summary>
    private int GetSpritePixelColor(int spriteIndex, int pixelInSprite)
    {
        var isMulticolor = _vic2Registers.IsSpriteMulticolor(spriteIndex);

        if (isMulticolor)
        {
            // Multicolor: 2 bits per pixel, 12 effective pixels
            var bitPairPosition = 11 - (pixelInSprite / 2);
            var shiftAmount = bitPairPosition * 2;
            var bitPair = (_spriteShiftRegisters[spriteIndex] >> shiftAmount) & 0x03;

            return bitPair switch
            {
                0 => -1, // Transparent
                1 => _vic2Registers.SpriteMulticolorColor0,
                2 => _vic2Registers.GetSpriteColor(spriteIndex),
                3 => _vic2Registers.SpriteMulticolorColor1,
                _ => -1,
            };
        }
        else
        {
            // Standard: 1 bit per pixel, 24 pixels
            var bitPosition = 23 - pixelInSprite;
            var bit = (_spriteShiftRegisters[spriteIndex] >> bitPosition) & 1;

            return bit == 1 ? _vic2Registers.GetSpriteColor(spriteIndex) : -1;
        }
    }

    private RgbColor RenderStandardTextModePixel(int displayX)
    {
        var charColumn = displayX / 8;
        var bitPosition = 7 - (displayX % 8);

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var isForeground = ((pixelData >> bitPosition) & 1) == 1;

            if (isForeground)
            {
                var fgColor = (_videoColorBuffer[charColumn] >> 8) & 0x0F;
                return _palette[fgColor];
            }

            return _palette[_vic2Registers.BackgroundColor0];
        }

        return _palette[_vic2Registers.BackgroundColor0];
    }

    private RgbColor RenderEcmTextModePixel(int displayX)
    {
        var charColumn = displayX / 8;
        var bitPosition = 7 - (displayX % 8);

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var isForeground = ((pixelData >> bitPosition) & 1) == 1;

            if (isForeground)
            {
                var fgColor = (_videoColorBuffer[charColumn] >> 8) & 0x0F;
                return _palette[fgColor];
            }

            // In ECM mode, upper 2 bits of character code select background color
            var charCode = (byte)(_videoColorBuffer[charColumn] & 0xFF);
            var bgSelect = (charCode >> 6) & 0x03;

            return bgSelect switch
            {
                0 => _palette[_vic2Registers.BackgroundColor0],
                1 => _palette[_vic2Registers.BackgroundColor1],
                2 => _palette[_vic2Registers.BackgroundColor2],
                3 => _palette[_vic2Registers.BackgroundColor3],
                _ => _palette[_vic2Registers.BackgroundColor0],
            };
        }

        return _palette[_vic2Registers.BackgroundColor0];
    }

    private RgbColor RenderMulticolorTextModePixel(int displayX)
    {
        var charColumn = displayX / 8;

        if (charColumn >= 0 && charColumn < 40)
        {
            var colorByte = (_videoColorBuffer[charColumn] >> 8) & 0x0F;

            // If bit 3 of color RAM is 0, render as standard text mode
            if ((colorByte & 0x08) == 0)
            {
                return RenderStandardTextModePixel(displayX);
            }

            // Multicolor mode: pixel pairs (half horizontal resolution)
            var pixelData = _graphicsDataBuffer[charColumn];
            var bitPairIndex = (displayX % 8) / 2;
            var bitPair = (pixelData >> (6 - bitPairIndex * 2)) & 0x03;

            return bitPair switch
            {
                0 => _palette[_vic2Registers.BackgroundColor0],
                1 => _palette[_vic2Registers.BackgroundColor1],
                2 => _palette[_vic2Registers.BackgroundColor2],
                3 => _palette[colorByte & 0x07], // Lower 3 bits of color RAM
                _ => _palette[_vic2Registers.BackgroundColor0],
            };
        }

        return _palette[_vic2Registers.BackgroundColor0];
    }

    private RgbColor RenderStandardBitmapModePixel(int displayX)
    {
        var charColumn = displayX / 8;
        var bitPosition = 7 - (displayX % 8);

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var isForeground = ((pixelData >> bitPosition) & 1) == 1;

            // In bitmap mode, screen RAM provides colors:
            // Upper nybble = foreground color, lower nybble = background color
            var screenData = (byte)(_videoColorBuffer[charColumn] & 0xFF);
            var fgColor = (screenData >> 4) & 0x0F;
            var bgColor = screenData & 0x0F;

            return isForeground ? _palette[fgColor] : _palette[bgColor];
        }

        return _palette[_vic2Registers.BackgroundColor0];
    }

    private RgbColor RenderMulticolorBitmapModePixel(int displayX)
    {
        var charColumn = displayX / 8;

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var bitPairIndex = (displayX % 8) / 2;
            var bitPair = (pixelData >> (6 - bitPairIndex * 2)) & 0x03;

            // Screen RAM and color RAM provide colors
            var screenData = (byte)(_videoColorBuffer[charColumn] & 0xFF);
            var colorRamData = (_videoColorBuffer[charColumn] >> 8) & 0x0F;

            return bitPair switch
            {
                0 => _palette[_vic2Registers.BackgroundColor0],
                1 => _palette[(screenData >> 4) & 0x0F], // Screen RAM upper nybble
                2 => _palette[screenData & 0x0F],        // Screen RAM lower nybble
                3 => _palette[colorRamData],             // Color RAM
                _ => _palette[_vic2Registers.BackgroundColor0],
            };
        }

        return _palette[_vic2Registers.BackgroundColor0];
    }

    private (RgbColor color, bool isForeground) RenderStandardTextModePixelWithFg(int displayX)
    {
        var charColumn = displayX / 8;
        var bitPosition = 7 - (displayX % 8);

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var isForeground = ((pixelData >> bitPosition) & 1) == 1;

            if (isForeground)
            {
                var fgColor = (_videoColorBuffer[charColumn] >> 8) & 0x0F;
                return (_palette[fgColor], true);
            }

            return (_palette[_vic2Registers.BackgroundColor0], false);
        }

        return (_palette[_vic2Registers.BackgroundColor0], false);
    }

    private (RgbColor color, bool isForeground) RenderEcmTextModePixelWithFg(int displayX)
    {
        var charColumn = displayX / 8;
        var bitPosition = 7 - (displayX % 8);

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var isForeground = ((pixelData >> bitPosition) & 1) == 1;

            if (isForeground)
            {
                var fgColor = (_videoColorBuffer[charColumn] >> 8) & 0x0F;
                return (_palette[fgColor], true);
            }

            // In ECM mode, upper 2 bits of character code select background color
            var charCode = (byte)(_videoColorBuffer[charColumn] & 0xFF);
            var bgSelect = (charCode >> 6) & 0x03;

            var bgColor = bgSelect switch
            {
                0 => _palette[_vic2Registers.BackgroundColor0],
                1 => _palette[_vic2Registers.BackgroundColor1],
                2 => _palette[_vic2Registers.BackgroundColor2],
                3 => _palette[_vic2Registers.BackgroundColor3],
                _ => _palette[_vic2Registers.BackgroundColor0],
            };

            return (bgColor, false);
        }

        return (_palette[_vic2Registers.BackgroundColor0], false);
    }

    private (RgbColor color, bool isForeground) RenderMulticolorTextModePixelWithFg(int displayX)
    {
        var charColumn = displayX / 8;

        if (charColumn >= 0 && charColumn < 40)
        {
            var colorByte = (_videoColorBuffer[charColumn] >> 8) & 0x0F;

            // If bit 3 of color RAM is 0, render as standard text mode
            if ((colorByte & 0x08) == 0)
            {
                return RenderStandardTextModePixelWithFg(displayX);
            }

            // Multicolor mode: pixel pairs (half horizontal resolution)
            var pixelData = _graphicsDataBuffer[charColumn];
            var bitPairIndex = (displayX % 8) / 2;
            var bitPair = (pixelData >> (6 - bitPairIndex * 2)) & 0x03;

            // In multicolor text mode, bit pairs 01, 10, 11 are considered foreground
            var isForeground = bitPair != 0;

            var color = bitPair switch
            {
                0 => _palette[_vic2Registers.BackgroundColor0],
                1 => _palette[_vic2Registers.BackgroundColor1],
                2 => _palette[_vic2Registers.BackgroundColor2],
                3 => _palette[colorByte & 0x07],
                _ => _palette[_vic2Registers.BackgroundColor0],
            };

            return (color, isForeground);
        }

        return (_palette[_vic2Registers.BackgroundColor0], false);
    }

    private (RgbColor color, bool isForeground) RenderStandardBitmapModePixelWithFg(int displayX)
    {
        var charColumn = displayX / 8;
        var bitPosition = 7 - (displayX % 8);

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var isForeground = ((pixelData >> bitPosition) & 1) == 1;

            var screenData = (byte)(_videoColorBuffer[charColumn] & 0xFF);
            var fgColor = (screenData >> 4) & 0x0F;
            var bgColor = screenData & 0x0F;

            return (isForeground ? _palette[fgColor] : _palette[bgColor], isForeground);
        }

        return (_palette[_vic2Registers.BackgroundColor0], false);
    }

    private (RgbColor color, bool isForeground) RenderMulticolorBitmapModePixelWithFg(int displayX)
    {
        var charColumn = displayX / 8;

        if (charColumn >= 0 && charColumn < 40)
        {
            var pixelData = _graphicsDataBuffer[charColumn];
            var bitPairIndex = (displayX % 8) / 2;
            var bitPair = (pixelData >> (6 - bitPairIndex * 2)) & 0x03;

            var screenData = (byte)(_videoColorBuffer[charColumn] & 0xFF);
            var colorRamData = (_videoColorBuffer[charColumn] >> 8) & 0x0F;

            // In multicolor bitmap mode, bit pairs 01, 10, 11 are considered foreground
            var isForeground = bitPair != 0;

            var color = bitPair switch
            {
                0 => _palette[_vic2Registers.BackgroundColor0],
                1 => _palette[(screenData >> 4) & 0x0F],
                2 => _palette[screenData & 0x0F],
                3 => _palette[colorRamData],
                _ => _palette[_vic2Registers.BackgroundColor0],
            };

            return (color, isForeground);
        }

        return (_palette[_vic2Registers.BackgroundColor0], false);
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

    /// <summary>
    /// Handles sprite memory access operations that run in parallel with other VIC operations
    /// </summary>
    private void RunSpriteMemoryAccess()
    {
        // Cycles 1-10: P-access for sprites 3-7, S-access for sprites with DMA enabled
        if (_lineCycleCount >= 1 && _lineCycleCount <= 10)
        {
            var spriteIndex = _lineCycleCount - 1;
            if (spriteIndex >= 3 && spriteIndex < 8)
            {
                PerformSpritePointerAccess(spriteIndex);
            }

            // S-access: fetch sprite data for sprites with DMA enabled
            for (var i = 0; i < 8; i++)
            {
                if (_spriteDmaEnabled[i] && _spriteDisplayEnabled[i])
                {
                    PerformSpriteDataAccess(i);
                }
            }
        }

        // Cycles 55-57: Check sprite Y position matches - enable DMA if sprite Y == raster line
        if (_lineCycleCount >= 55 && _lineCycleCount <= 57)
        {
            for (var i = 0; i < 8; i++)
            {
                if (_vic2Registers.IsSpriteEnabled(i))
                {
                    var spriteY = _vic2Registers.GetSpriteY(i);
                    if (spriteY == (_currentScanLine & 0xFF))
                    {
                        _spriteDmaEnabled[i] = true;
                        _spriteDisplayEnabled[i] = true;
                        _spriteCurrentLine[i] = 0;
                        _spriteYExpandFlipFlop[i] = false;
                        _activeSpriteMask |= (byte)(1 << i);
                    }
                }
            }
        }

        // Cycles 58-60: P-access for sprites 0-2
        if (_lineCycleCount >= 58 && _lineCycleCount <= 60)
        {
            var spriteIndex = _lineCycleCount - 58;
            if (spriteIndex < 3)
            {
                PerformSpritePointerAccess(spriteIndex);
            }
        }
    }

    /// <summary>
    /// Performs P-access: fetches sprite pointer from screen memory + $3F8 + sprite index
    /// </summary>
    private void PerformSpritePointerAccess(int spriteIndex)
    {
        var screenBase = (ushort)(_vic2Registers.ScreenPointer << 10);
        var pointerAddress = (ushort)(screenBase + 0x3F8 + spriteIndex);
        _spritePointers[spriteIndex] = ReadRam(pointerAddress);
    }

    /// <summary>
    /// Performs S-access: fetches 3 bytes of sprite data for the current line
    /// </summary>
    private void PerformSpriteDataAccess(int spriteIndex)
    {
        // Sprite data address = pointer * 64 + line * 3
        var baseAddress = (ushort)(_spritePointers[spriteIndex] * 64);
        var lineOffset = (ushort)(_spriteCurrentLine[spriteIndex] * 3);

        for (var i = 0; i < 3; i++)
        {
            var address = (ushort)(baseAddress + lineOffset + i);
            _spriteDataBuffer[spriteIndex, i] = ReadRam(address);
        }

        // Load shift register with 24 bits of sprite data
        _spriteShiftRegisters[spriteIndex] = (uint)(
            (_spriteDataBuffer[spriteIndex, 0] << 16) |
            (_spriteDataBuffer[spriteIndex, 1] << 8) |
            _spriteDataBuffer[spriteIndex, 2]
        );
    }

    /// <summary>
    /// Advances sprite state at the end of each scanline
    /// </summary>
    private void AdvanceSpriteState()
    {
        for (var i = 0; i < 8; i++)
        {
            if (!_spriteDmaEnabled[i])
            {
                continue;
            }

            var isYExpanded = _vic2Registers.IsSpriteYExpanded(i);

            if (isYExpanded)
            {
                // For Y-expanded sprites, toggle flip-flop and only advance every other line
                _spriteYExpandFlipFlop[i] = !_spriteYExpandFlipFlop[i];
                if (_spriteYExpandFlipFlop[i])
                {
                    continue; // Skip advancement on odd lines
                }
            }

            _spriteCurrentLine[i]++;

            // Sprite is 21 lines tall
            if (_spriteCurrentLine[i] >= 21)
            {
                _spriteDmaEnabled[i] = false;
                _spriteDisplayEnabled[i] = false;
                _spriteCurrentLine[i] = 0;
                _activeSpriteMask &= (byte)~(1 << i);
            }
        }
    }
}