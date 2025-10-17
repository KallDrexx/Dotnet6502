using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.ROM;

namespace Dotnet6502.Nes;

/// <summary>
/// Implementation of the NES PPU
/// </summary>
public class Ppu : IMemoryDevice
{
    private enum CurrentDotLocation
    {
        InDisplayableArea, InHBlank, InPostRender, StartsNewDisplayableScanline, StartsPostRender, StartsVBlank,
        StartsNewScanlineInVBlank, InVBlank, StartsPreRender, InPreRender, StartsFirstDisplayableScanLine
    }
    
    private const int PpuCyclesPerScanline = 341;
    private const int DisplayableScanLines = 240;
    private const int PostRenderBlankingLines = 1;
    private const int PreRenderBlankingLines = 1;
    private const int VBlankLinesPostNmi = 20;
    private const int HBlankCycles = 85;
    private const int PpuCyclesPerCpuCycle = 3;
    private const int TotalScanLines =
        DisplayableScanLines +
        PostRenderBlankingLines +
        VBlankLinesPostNmi +
        PreRenderBlankingLines;

    private const int DisplayableWidth = PpuCyclesPerScanline - HBlankCycles;
    private const int NameTableSize = 0x3c0;

    private readonly RgbColor[] _systemPalette =
    [
        new(0x80, 0x80, 0x80), new(0x80, 0x80, 0x80), new(0x80, 0x80, 0x80),
        new(0x80, 0x80, 0x80), new(0x80, 0x80, 0x80), new(0xC7, 0x00, 0x28),
        new(0xBA, 0x06, 0x00), new(0x8C, 0x17, 0x00), new(0x5C, 0x2F, 0x00),
        new(0x10, 0x45, 0x00), new(0x05, 0x4A, 0x00), new(0x00, 0x47, 0x2E),
        new(0x00, 0x41, 0x66), new(0x00, 0x00, 0x00), new(0x05, 0x05, 0x05),
        new(0x05, 0x05, 0x05), new(0xC7, 0xC7, 0xC7), new(0x00, 0x77, 0xFF),
        new(0x21, 0x55, 0xFF), new(0x82, 0x37, 0xFA), new(0xEB, 0x2F, 0xB5),
        new(0xFF, 0x29, 0x50), new(0xFF, 0x22, 0x00), new(0xD6, 0x32, 0x00),
        new(0xC4, 0x62, 0x00), new(0x35, 0x80, 0x00), new(0x05, 0x8F, 0x00),
        new(0x00, 0x8A, 0x55), new(0x00, 0x99, 0xCC), new(0x21, 0x21, 0x21),
        new(0x09, 0x09, 0x09), new(0x09, 0x09, 0x09), new(0xFF, 0xFF, 0xFF),
        new(0x0F, 0xD7, 0xFF), new(0x69, 0xA2, 0xFF), new(0xD4, 0x80, 0xFF),
        new(0xFF, 0x45, 0xF3), new(0xFF, 0x61, 0x8B), new(0xFF, 0x88, 0x33),
        new(0xFF, 0x9C, 0x12), new(0xFA, 0xBC, 0x20), new(0x9F, 0xE3, 0x0E),
        new(0x2B, 0xF0, 0x35), new(0x0C, 0xF0, 0xA4), new(0x05, 0xFB, 0xFF),
        new(0x5E, 0x5E, 0x5E), new(0x0D, 0x0D, 0x0D), new(0x0D, 0x0D, 0x0D),
        new(0xFF, 0xFF, 0xFF), new(0xA6, 0xFC, 0xFF), new(0xB3, 0xEC, 0xFF),
        new(0xDA, 0xAB, 0xEB), new(0xFF, 0xA8, 0xF9), new(0xFF, 0xAB, 0xB3),
        new(0xFF, 0xD2, 0xB0), new(0xFF, 0xEF, 0xA6), new(0xFF, 0xF7, 0x9C),
        new(0xD7, 0xE8, 0x95), new(0xA6, 0xED, 0xAF), new(0xA2, 0xF2, 0xDA),
        new(0x99, 0xFF, 0xFC), new(0xDD, 0xDD, 0xDD), new(0x11, 0x11, 0x11),
        new(0x11, 0x11, 0x11)
    ];

    // Registers
    internal PpuCtrl PpuCtrl { get; }
    internal PpuStatus PpuStatus { get; }
    internal PpuMask PpuMask { get; }
    internal ushort PpuAddr { get; private set; }
    private byte _oamAddrRegister;
    private byte _xScrollRegister, _yScrollRegister;
    private bool _wRegister;
    private ushort _vRegister;
    private byte _tRegister;
    private byte _xRegister;
    private byte _readBuffer;
    private readonly MirroringType _mirroringType;

    private readonly INesDisplay _nesDisplay;
    private readonly RgbColor[] _framebuffer = new RgbColor[DisplayableWidth * DisplayableScanLines];
    private readonly byte[] _memory = new byte[0x4000];
    private readonly byte[] _oamMemory = new byte[0x100];
    private readonly ScanLineRenderInfo _scanLineRenderInfo = new();
    private int _pixelIndex;
    private int _currentScanLineCycle;
    private int _currentScanLine; // zero based index of what scan line we are currently at
    private bool _hasNmiTriggered; // Has NMI been marSked as to be triggered this frame

    public int CurrentScanLine => _currentScanLine;

    uint IMemoryDevice.Size => 8;

    /// <summary>
    /// Not a continuous block of ram
    /// </summary>
    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    public Ppu(byte[] chrRomData, MirroringType mirroringType, INesDisplay nesDisplay)
    {
        _nesDisplay = nesDisplay;
        PpuCtrl = new PpuCtrl();
        PpuStatus = new PpuStatus();
        PpuMask = new PpuMask();
        _mirroringType = mirroringType;

        if (chrRomData.Length > 0)
        {
            if (chrRomData.Length is not (0x2000 or 0x4000))
            {
                var message = $"Expected chrRomData to be 0x2000 or 0x4000 bytes, but was 0x{chrRomData.Length:X4}";
                throw new ArgumentException(message);
            }

            for (var x = 0; x < chrRomData.Length; x++)
            {
                _memory[x] = chrRomData[x];
            }
        }
    }

    /// <summary>
    /// Executes the next Ppu step based on the number of CPU cycles about to be executed.
    /// </summary>
    /// <returns>True if this execution triggered NMI (NMI enable & vblank PPU Status)</returns>
    public bool RunNextStep(int cpuCycleCount)
    {
        var ppuCycles = cpuCycleCount * PpuCyclesPerCpuCycle;

        for (var x = 0; x < ppuCycles; x++)
        {
            RunSinglePpuCycle();
        }

        var triggerNmi = PpuCtrl.NmiEnable == PpuCtrl.NmiEnableValue.On && PpuStatus.VBlankFlag && !_hasNmiTriggered;
        if (triggerNmi)
        {
            _hasNmiTriggered = true;
        }

        return triggerNmi;
    }

    /// <summary>
    /// Process a request from the CPU to write to a PPU owned memory address
    /// </summary>
    public void Write(ushort address, byte value)
    {
        var byteNumber = address % 8;
        switch (byteNumber)
        {
            case 0:
                PpuCtrl.UpdateFromByte(value);
                break;

            case 1:
                PpuMask.UpdateFromByte(value);
                break;

            case 2:
                break; // PPU Status not writable

            case 3:
                _oamAddrRegister = value;
                break;

            case 4:
                _oamMemory[_oamAddrRegister] = value;
                _oamAddrRegister++;

                break;

            case 5:
                if (_wRegister)
                {
                    _yScrollRegister = value;
                }
                else
                {
                    _xScrollRegister = value;
                }

                _wRegister = !_wRegister;
                break;

            case 6:
                if (_wRegister)
                {
                    PpuAddr = (ushort)((PpuAddr & 0xFF00) | value);
                }
                else
                {
                    // PPUADDR is only 14 bits, so clear out the two high bits
                    var maskedValue = value & 0x3F;
                    PpuAddr = (ushort)((PpuAddr & 0x00FF) | (maskedValue << 8));
                }

                _wRegister = !_wRegister;
                _vRegister = PpuAddr;

                // Prime the read buffer
                _readBuffer = _memory[PpuAddr];
                break;

            case 7:
                if (PpuAddr >= 0x3F00)
                {
                    var mirroredAddress = GetPaletteMemoryLocation(PpuAddr);

                    // Entry 0 of each palette is mirrored between sprite and background
                    if ((mirroredAddress & 0x000F) is 0x0 or 0x4 or 0x8 or 0xC)
                    {
                        var bgAddress = mirroredAddress & 0xFF0F;
                        var spriteAddress = mirroredAddress & 0xFF1F;
                        _memory[bgAddress] = value;
                        _memory[spriteAddress] = value;
                    }
                    else
                    {
                        _memory[mirroredAddress] = value;
                    }
                }
                else
                {
                    _memory[PpuAddr] = value;
                }

                if (PpuCtrl.VRamAddressIncrement == PpuCtrl.VRamAddressIncrementValue.Add1Across)
                {
                    PpuAddr += 1;
                }
                else
                {
                    PpuAddr += 32;
                }

                _vRegister = PpuAddr;

                break;

            default:
                throw new NotSupportedException(byteNumber.ToString());
        }
    }

    /// <summary>
    /// Process a request from the CPU to read from a PPU owned memory address
    /// </summary>
    public byte Read(ushort address)
    {
        if (address == 0x4014)
        {
            return 0;
        }

        var byteNumber = address % 8;
        switch (byteNumber)
        {
            case 0:
                return 0; // PpuCtl Not readable

            case 1:
                return 0; // PpuMask not readable

            case 2:
                var result = PpuStatus.ToByte();
                PpuStatus.VBlankFlag = false; // Clear vblank on read
                _wRegister = false; // w register always gets cleared on status read
                return result;

            case 3:
                return 0; // OamAddr not readable

            case 4:
                return _oamMemory[_oamAddrRegister];

            case 5:
                return 0; // PPUSCROLL not readable

            case 6:
                return 0; // PPUADDR not readable

            case 7:
                // PPU reads up to 0x3F00 are delayed one read
                byte value;
                if (PpuAddr < 0x3F00)
                {
                    value = _readBuffer;
                    _readBuffer = _memory[PpuAddr];
                }
                else
                {
                    var mirroredAddress = GetPaletteMemoryLocation(PpuAddr);
                    value = _memory[mirroredAddress];
                }

                if (PpuCtrl.VRamAddressIncrement == PpuCtrl.VRamAddressIncrementValue.Add1Across)
                {
                    PpuAddr += 1;
                }
                else
                {
                    PpuAddr += 32;
                }

                return value;

            default:
                throw new NotSupportedException(byteNumber.ToString());
        }
    }

    private static ushort GetPaletteMemoryLocation(ushort address)
    {
        // $3F20-$3FFF mirrors $3F00-$3F1F
        while (address >= 0x3F20)
        {
            address -= 0x20;
        }

        return address;
    }

    private void RunSinglePpuCycle()
    {
        _currentScanLineCycle++;

        var currentLocation = GetCurrentDotLocation();
        switch (currentLocation)
        {
            case CurrentDotLocation.InDisplayableArea:
                _pixelIndex++;
                _scanLineRenderInfo.IncrementPixel();
                HandleDisplayablePixelLogic();
                break;

            case CurrentDotLocation.InVBlank:
            case CurrentDotLocation.InHBlank:
            case CurrentDotLocation.InPostRender:
            case CurrentDotLocation.InPreRender:
                break; // Nothing to do

            case CurrentDotLocation.StartsNewDisplayableScanline:
                _currentScanLineCycle = 0;
                _currentScanLine++;
                _pixelIndex++;
                RebuildScanLineRenderInfo();
                HandleDisplayablePixelLogic();
                break;

            case CurrentDotLocation.StartsNewScanlineInVBlank:
            case CurrentDotLocation.StartsPostRender:
                _currentScanLineCycle = 0;
                _currentScanLine++;
                break;

            case CurrentDotLocation.StartsPreRender:
                _currentScanLineCycle = 0;
                _currentScanLine++;
                PpuStatus.VBlankFlag = false;
                break;

            case CurrentDotLocation.StartsFirstDisplayableScanLine:
                _currentScanLineCycle = 0;
                _currentScanLine = 0;
                _hasNmiTriggered = false;
                _pixelIndex = 0;
                RebuildScanLineRenderInfo();
                HandleDisplayablePixelLogic();
                break;

            case CurrentDotLocation.StartsVBlank:
                _currentScanLineCycle = 0;
                _currentScanLine++;
                PpuStatus.VBlankFlag = true;
                PpuStatus.Sprite0HitFlag = false;
                RenderFrame();
                break;

            default:
                throw new NotSupportedException(currentLocation.ToString());
        }

        ResetOamData();
    }

    private void HandleDisplayablePixelLogic()
    {
        DrawNextPixel();
    }

    private void DrawNextPixel()
    {
        var bgColor = DrawBackgroundPixel();

        _framebuffer[_pixelIndex] = bgColor ?? new RgbColor(0, 0, 0);
    }

    private RgbColor? DrawBackgroundPixel()
    {
        if (!PpuMask.EnableBackgroundRendering ||
            (_currentScanLineCycle < 8 && !PpuMask.ShowBackgroundInLeftmost8PixelsOfScreen))
        {
            return null;
        }

        // Each tile is 8x8 pixels with 2 bits per pixel, but you have one bit in the first
        // set of 8 bytes and the second bit in the second set of 8 bytes. So we need to isolate out the
        // correct upper and lower bit values
        var tileX = _scanLineRenderInfo.ColumnInTile;
        var tileY = _scanLineRenderInfo.RowInTile;
        var tileData = _scanLineRenderInfo.TileData[_scanLineRenderInfo.CurrentTileIndex];
        var plane0 = tileData.Span[tileY] >> (7 - tileX); // MSB is left most pixel
        var plane1 = tileData.Span[tileY + 8] >> (7 - tileX);
        var value = ((1 & plane1) << 1) | (1 & plane0);

        var paletteStart = 1 + _scanLineRenderInfo.PaletteIndices[_scanLineRenderInfo.CurrentTileIndex] * 4;
        var paletteTable = _memory.AsSpan()[0x3F00..];

        var palette = value switch
        {
            0 => paletteTable[0],
            1 => paletteTable[paletteStart],
            2 => paletteTable[paletteStart + 1],
            3 => paletteTable[paletteStart + 2],
            _ => throw new ArgumentOutOfRangeException(value.ToString())
        };

        // Sprite 0 hit flag should only occur if the background is opaque. However, there's a weird issue
        // in SMB where after 2 screens worth of space the name table flips back and forth incorrectly
        // between 2000 and 2400, which causes sprite 0 hit flag to never be set (because the background there
        // is transparent) and thus it locks up.
        //
        // This ultimately seems to be caused due to this PPU implementation being a 'Logical" reproduction
        // and not emulating the actual hardware shift registers.
        if (/*value != 0 && */ !PpuStatus.Sprite0HitFlag)
        {
            var targetX = _currentScanLineCycle;
            var targetY = _currentScanLine;

            // check if this is a sprite 0 hit
            var spriteX = _oamMemory[3];
            var spriteY = _oamMemory[0];
            if (targetX >= spriteX && targetX < spriteX + 8 &&
                targetY >= spriteY && targetY < spriteY + 8)
            {
                // Check if this is a sprite 0 hit
                var spritePixelX = targetX - spriteX;
                var spritePixelY = targetY - spriteY;
                var attributes = GetSpriteAttribute(0);
                var color = GetSpriteColor(attributes, spritePixelX, spritePixelY);
                if (color != null)
                {
                    PpuStatus.Sprite0HitFlag = true;
                }
            }
        }

        return _systemPalette[palette];
    }

    private SpriteAttributes GetSpriteAttribute(int spriteIndex)
    {
        var tileIndex = _oamMemory[spriteIndex + 1];
        var flipVertical = ((_oamMemory[spriteIndex + 2] >> 7) & 1) == 1;
        var flipHorizontal = ((_oamMemory[spriteIndex + 2] >> 6) & 1) == 1;
        var paletteIndex = _oamMemory[spriteIndex + 2] & 0b11;
        var paletteStartOffset = 0x11 + paletteIndex * 4;
        var priority = ((_oamMemory[spriteIndex + 2] >> 5) & 1) == 1; // 0 in front, 1 behind background

        var bank = PpuCtrl.SpritePatternTableAddressFor8X8 switch
        {
            PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex0000 => 0x0000,
            PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex1000 => 0x1000,
            _ => throw new NotSupportedException(PpuCtrl.SpritePatternTableAddressFor8X8.ToString()),
        };

        var tileStart = bank + tileIndex * 16;
        var tileData = _memory.AsMemory(tileStart, 16);

        return new SpriteAttributes(flipVertical, flipHorizontal, tileData, paletteStartOffset, priority);
    }

    private RgbColor? GetSpriteColor(SpriteAttributes attributes, int spritePixelX, int spritePixelY)
    {
        // Account for flipping
        if (attributes.FlipHorizontal)
        {
            spritePixelX = 7 - spritePixelX;
        }

        if (attributes.FlipVertical)
        {
            spritePixelY = 7 - spritePixelY;
        }

        // Read the specific pixel from the tile data
        var upper = attributes.TileData.Span[spritePixelY];
        var lower = attributes.TileData.Span[spritePixelY + 8];

        // Extract the bit at position tilePixelX (where 7 is leftmost, 0 is rightmost)
        var bitPosition = 7 - spritePixelX;
        var value = (((lower >> bitPosition) & 1) << 1) | ((upper >> bitPosition) & 1);
        if (value == 0)
        {
            return null; // transparent
        }

        var palette = _memory.AsSpan(0x3F00 + attributes.PaletteStartOffset);
        return _systemPalette[palette[value - 1]];
    }

    private ushort GetNameTable(int scrolledX, int scrolledY)
    {
        ushort nameTableAddress = PpuCtrl.BaseNameTableAddress switch
        {
            PpuCtrl.BaseNameTableAddressValue.Hex2000 => 0x2000,
            PpuCtrl.BaseNameTableAddressValue.Hex2400 => 0x2400,
            PpuCtrl.BaseNameTableAddressValue.Hex2800 => 0x2800,
            PpuCtrl.BaseNameTableAddressValue.Hex2C00 => 0x2C00,
            _ => throw new NotSupportedException(PpuCtrl.BaseNameTableAddress.ToString()),
        };

        // Adjust to the correct name table based on the passed in scroll adjustments
        var leftNameTable = scrolledX / 256 == 0;
        var topNameTable = scrolledY / 240 == 0;

        if (!leftNameTable)
        {
            nameTableAddress += 0x400;
        }

        if (!topNameTable)
        {
            nameTableAddress += 0x800;
        }

        if (nameTableAddress >= 0x3000)
        {
            nameTableAddress -= 0x1000;
        }

        // Next account for mirroring
        if (_mirroringType == MirroringType.Horizontal)
        {
            if (nameTableAddress is 0x2400 or 0x2C00)
            {
                nameTableAddress -= 0x400;
            }
        }
        else if (_mirroringType == MirroringType.Vertical)
        {
            if (nameTableAddress >= 0x2800)
            {
                nameTableAddress -= 0x800;
            }
        }

        return nameTableAddress;
    }

    private void RenderFrame()
    {
        DrawSprites();

        _nesDisplay.RenderFrame(_framebuffer);

        for (var x = 0; x < _framebuffer.Length; x++)
        {
            _framebuffer[x] = new RgbColor(0, 0, 0);
        }
    }

    private void DrawSprites()
    {
        // Draw sprites in reverse order to ensure lower-indexed sprites are drawn on top
        for (var index = 256 - 4; index >= 0; index -= 4)
        {
            var attributes = GetSpriteAttribute(index);
            var tileX = _oamMemory[index + 3];
            var tileY = _oamMemory[index];

            for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
            {
                var color = GetSpriteColor(attributes, x, y);
                if (color != null)
                {
                    var screenX = tileX + x;
                    var screenY = tileY + y;
                    if (screenX >= DisplayableWidth || screenY >= DisplayableScanLines)
                    {
                        continue;
                    }

                    // Sprite always has priority as foreground and lower index
                    SetPixel(screenX, screenY, color.Value);
                }
            }
        }
    }

    private void SetPixel(int x, int y, RgbColor color)
    {
        if (x < 0 || x >= DisplayableWidth || y < 0 || y >= DisplayableScanLines)
        {
            return;
        }

        var index = y * DisplayableWidth + x;
        _framebuffer[index] = color;
    }

    private void ResetOamData()
    {
        // OAMADDR is set to 0 during each of ticks 257–320 (the sprite tile loading interval)
        // of the pre-render and visible scanlines.
        var isInPreRender = _currentScanLine == TotalScanLines - 1;
        var isInVisibleRegion = _currentScanLine < DisplayableScanLines;
        var isWithinCycleBounds = _currentScanLineCycle is >= 257 and <= 320;

        if ((isInPreRender || isInVisibleRegion) && isWithinCycleBounds)
        {
            _oamAddrRegister = 0;
        }
    }

    private CurrentDotLocation GetCurrentDotLocation()
    {
        // NOTE: Assumes cycle count has already been incremented by one, but nothing else has been incremented
        if (_currentScanLineCycle > PpuCyclesPerScanline)
        {
            var message = $"Expected cycle count to never get past {PpuCyclesPerScanline}, but it was {_currentScanLineCycle}";
            throw new InvalidOperationException(message);
        }

        if (_currentScanLineCycle == PpuCyclesPerScanline)
        {
            // Starting a new scanline. Scan line count hasn't been incremented yet
            if (_currentScanLine < DisplayableScanLines - 1)
            {
                return CurrentDotLocation.StartsNewDisplayableScanline;
            }

            if (_currentScanLine == DisplayableScanLines - 1)
            {
                return CurrentDotLocation.StartsPostRender;
            }

            if (_currentScanLine == DisplayableScanLines + PostRenderBlankingLines - 1)
            {
                return CurrentDotLocation.StartsVBlank;
            }

            if (_currentScanLine == TotalScanLines - PreRenderBlankingLines - 1)
            {
                return CurrentDotLocation.StartsPreRender;
            }

            if (_currentScanLine == TotalScanLines - 1)
            {
                return CurrentDotLocation.StartsFirstDisplayableScanLine;
            }

            return CurrentDotLocation.StartsNewScanlineInVBlank;
        }

        // Within a scanline
        if (_currentScanLine == DisplayableScanLines)
        {
            return CurrentDotLocation.InPostRender;
        }

        if (_currentScanLine == TotalScanLines - PreRenderBlankingLines - 1)
        {
            return CurrentDotLocation.InPreRender;
        }

        if (_currentScanLine > DisplayableScanLines)
        {
            return CurrentDotLocation.InVBlank;
        }

        if (_currentScanLineCycle < DisplayableWidth)
        {
            return CurrentDotLocation.InDisplayableArea;
        }

        return CurrentDotLocation.InHBlank;
    }

    private void RebuildScanLineRenderInfo()
    {
        const int tileSizeInBytes = 16;
        const int tileWidthInPixels = 8;
        const int columnsPerNameTable = 32;

        var scrolledX = _xScrollRegister % 512;
        var scrolledY = (_currentScanLine + _yScrollRegister) % 480;
        var nameTableAddress = GetNameTable(scrolledX, scrolledY);

        var pixelXInNameTable = scrolledX % DisplayableWidth;
        var pixelYInNameTable = scrolledY % DisplayableScanLines;
        var tileColumn = pixelXInNameTable / 8;
        var tileRow = pixelYInNameTable / 8;
        var tileByteOffset = tileRow * 32 + tileColumn;

        // Each tile is 8x8 pixels with 2 bits per pixel, but you have one bit in the first
        // set of 8 bytes and the second bit in the second set of 8 bytes. So we need to isolate out the
        // correct upper and lower bit values
        var pixelXInTile = pixelXInNameTable % 8;
        var pixelYInTile = pixelYInNameTable % 8;

        ushort backgroundTableAddress = PpuCtrl.BackgroundPatternTableAddress switch
        {
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex0000 => 0x0000,
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex1000 => 0x1000,
            _ => throw new NotSupportedException(PpuCtrl.BackgroundPatternTableAddress.ToString()),
        };

        // Initial setup
        _scanLineRenderInfo.TileData.Clear();
        _scanLineRenderInfo.PaletteIndices.Clear();
        _scanLineRenderInfo.ColumnInTile = pixelXInTile;
        _scanLineRenderInfo.RowInTile = pixelYInTile;
        _scanLineRenderInfo.CurrentTileIndex = 0;

        for (var x = 0; x < DisplayableWidth + tileWidthInPixels; x += tileWidthInPixels)
        {
            if (x != 0)
            {
                scrolledX += tileWidthInPixels;
                tileColumn++;
                tileByteOffset++;

                if (tileColumn == columnsPerNameTable)
                {
                    // Got to the end of the name table
                    nameTableAddress = GetNameTable(scrolledX, scrolledY);
                    tileColumn = 0;
                    tileByteOffset = tileRow * columnsPerNameTable;
                }
            }

            var tileIndex = _memory[nameTableAddress + tileByteOffset];
            var tileStart = backgroundTableAddress + tileIndex * tileSizeInBytes;
            var tileEnd = tileStart + tileSizeInBytes;
            _scanLineRenderInfo.TileData.Add(_memory.AsMemory(tileStart..tileEnd));

            var paletteIndex = GetPaletteIndex(tileColumn, tileRow, nameTableAddress);
            _scanLineRenderInfo.PaletteIndices.Add(paletteIndex);
        }
    }

    private byte GetPaletteIndex(int tileColumn, int tileRow, ushort nameTableAddress)
    {
        var attributeTableIndex = tileRow / 4 * 8 + tileColumn / 4;
        var attributeTableLocation = nameTableAddress + NameTableSize;
        var attributeByteLocation = attributeTableLocation + attributeTableIndex;
        var attributeByte = _memory[attributeByteLocation];
        return (tileColumn % 4 / 2, tileRow % 4 / 2) switch
        {
            (0, 0) => (byte)(attributeByte & 0b11),
            (1, 0) => (byte)((attributeByte >> 2) & 0b11),
            (0, 1) => (byte)((attributeByte >> 4) & 0b11),
            (1, 1) => (byte)((attributeByte >> 6) & 0b11),
            _ => throw new NotSupportedException(),
        };
    }

    private record SpriteAttributes(
        bool FlipVertical,
        bool FlipHorizontal,
        Memory<byte> TileData,
        int PaletteStartOffset,
        bool IsBehindBackground);

    private class ScanLineRenderInfo
    {
        public List<Memory<byte>> TileData { get; } = new(33);
        public List<byte> PaletteIndices { get; } = new(33);
        public int CurrentTileIndex { get; set; }
        public int ColumnInTile { get; set; }
        public int RowInTile { get; set; }

        public void IncrementPixel()
        {
            ColumnInTile++;
            if (ColumnInTile == 8)
            {
                ColumnInTile = 0;
                CurrentTileIndex++;
            }
        }
    }
}