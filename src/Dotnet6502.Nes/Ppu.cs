using NESDecompiler.Core.ROM;

namespace Dotnet6502.Nes;

/// <summary>
/// Implementation of the NES PPU
/// </summary>
public class Ppu
{
    private readonly record struct Sprite(byte PatternLow, byte PatternHigh, byte Attributes, byte XCounter);

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
    private byte _oamAddrRegister;
    private byte _xScrollRegister, _yScrollRegister;
    private byte _xRegister;
    private byte _readBuffer;
    private readonly MirroringType _mirroringType;

    private readonly INesDisplay _nesDisplay;
    private readonly RgbColor[] _framebuffer = new RgbColor[DisplayableWidth * DisplayableScanLines];
    private readonly byte[] _memory = new byte[0x4000];
    private readonly byte[] _oamMemory = new byte[0x100];
    private readonly ScanLineRenderInfo _scanLineRenderInfo = new();
    private int _pixelIndex;
    private int _cycle;
    private int _scanline;
    private bool _isOddFrame;
    private bool _hasNmiTriggered; // Has NMI been marSked as to be triggered this frame

    internal ushort PpuAddr => _vRegister.RawValue;

    /// <summary>
    /// First or second write toggle
    /// </summary>
    private bool _wRegister;

    /// <summary>
    /// Current vram address (15 bits)
    /// </summary>
    private VramRegister _vRegister;

    /// <summary>
    /// Temporary vram address (15 bits)
    /// </summary>
    private VramRegister _tRegister;

    /// <summary>
    /// Fine X scroll (3 bits)
    /// </summary>
    private byte _fineX; // Fine X scroll (3 bits)

    /// <summary>
    /// Low bitplane shift register
    /// </summary>
    private ushort _bgPatternShiftLow;

    /// <summary>
    /// High bitplane shift register
    /// </summary>
    private ushort _bgPatternShiftHigh;

    /// <summary>
    /// Low attribute shift register
    /// </summary>
    private ushort _bgAttributeShiftLow;

    /// <summary>
    /// High attribute shift register
    /// </summary>
    private ushort _bgAttributeShiftHigh;

    private byte _nextTileIdLatch;
    private byte _nextTileAttributeLatch;
    private byte _nextTileLowLatch;
    private byte _nextTileHighLatch;

    /// <summary>
    /// The first 8 sprites on the current scan line
    /// </summary>
    private readonly Sprite[] _sprites = new Sprite[8];


    internal int Scanline => _scanline;

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
    public void ProcessMemoryWrite(ushort address, byte value)
    {
        var byteNumber = address % 8;
        switch (byteNumber)
        {
            // PPUCTRL
            case 0:
                PpuCtrl.UpdateFromByte(value);

                var nameTableSelect = (byte)(value & 0b11);
                _tRegister.NameTableSelect = nameTableSelect;
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

            // PPUSCROLL
            case 5:
                if (_wRegister)
                {
                    _tRegister.CoarseXScroll = (byte)((value & 0b11111000) >> 3);
                    _fineX = (byte)(value & 0b111);
                    _wRegister = false;
                }
                else
                {
                    _tRegister.FineYScroll = (byte)(value & 0b111);;
                    _tRegister.CoarseYScroll = (byte)((value & 0b11111000) >> 3);
                    _wRegister = true;
                }
                break;

            // PPUADDR
            case 6:
                if (_wRegister)
                {
                    var data = value & 0xFF;
                    _tRegister.RawValue = (ushort)((_tRegister.RawValue & 0xFF00) | data);
                    _vRegister.RawValue = _tRegister.RawValue;
                    _wRegister = false;
                }
                else
                {
                    var data = value & 0b0011111;
                    _tRegister.RawValue = (ushort)((_tRegister.RawValue & 0x00FF) | (data << 8));
                    _wRegister = true;
                }

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
                    _vRegister.IncrementX();
                }
                else
                {
                    _vRegister.IncrementY();
                }

                break;

            default:
                throw new NotSupportedException(byteNumber.ToString());
        }
    }

    /// <summary>
    /// Process a request from the CPU to read from a PPU owned memory address
    /// </summary>
    public byte ProcessMemoryRead(ushort address)
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
                    _vRegister.IncrementX();
                }
                else
                {
                    _vRegister.IncrementY();
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

    private void RunSingleCycle()
    {
        // Visible and pre-render scan lines. While nothing gets rendered on a pre-render scan line
        // memory access and shift register operations are still performed.
        if (_scanline >= -1 && _scanline < 240)
        {
            // Background rendering
            if (_cycle >= 1 && _cycle <= DisplayableWidth)
            {
                ShiftBackgroundRegisters();

                // Each of visible scanlines we fetch one piece of data. Each fetch takes 2 cycles
                switch (_cycle % 8)
                {
                    case 1:
                        FetchNameTableByte();
                        break;

                    case 3:
                        FetchAttributeByte();
                        break;

                    case 5:
                        FetchPatternTileLow();
                        break;

                    case 7:
                        FetchPatternTileHigh();
                        break;
                }
            }

            // Sprite fetches for next scanline
            if (_cycle >= 257 && _cycle <= 320)
            {
                if (_cycle == 257)
                {
                    _vRegister.IncrementY();
                }

                FetchSpriteData();
            }
        }
    }

    private void FetchNameTableByte()
    {
        var address = (ushort)(0x2000 | (_vRegister.RawValue & 0x0FFF));
        _nextTileIdLatch = _memory[address];
    }

    private void FetchAttributeByte()
    {
        var address = (ushort)(0x23C0 | (_vRegister.RawValue & 0x0C00) |
                               ((_vRegister.RawValue >> 4) & 0x38) |
                               ((_vRegister.RawValue >> 2) & 0x07));

        _nextTileAttributeLatch = _memory[address];
    }

    private void FetchPatternTileLow()
    {
        var patternTableAddress = (PpuCtrl.RawByte & 0x10) << 8;
        var fineY = _vRegister.FineYScroll & 0x07;
        var address = patternTableAddress + (_nextTileIdLatch * 16) + fineY;
        _nextTileLowLatch = _memory[address];
    }

    private void FetchPatternTileHigh()
    {
        var patternTableAddress = (PpuCtrl.RawByte & 0x10) << 8;
        var fineY = _vRegister.FineYScroll & 0x07;
        var address = patternTableAddress + (_nextTileIdLatch * 16) + fineY + 8;
        _nextTileHighLatch = _memory[address];
    }

    private void ShiftBackgroundRegisters()
    {
        _bgPatternShiftLow <<= 1;
        _bgPatternShiftHigh <<= 1;
        _bgAttributeShiftLow <<= 1;
        _bgAttributeShiftHigh <<= 1;
    }

    private void FetchSpriteData()
    {
        throw new NotImplementedException();
    }

    private void RunSinglePpuCycle()
    {
        _cycle++;

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
                _cycle = 0;
                _scanline++;
                _pixelIndex++;
                RebuildScanLineRenderInfo();
                HandleDisplayablePixelLogic();
                break;

            case CurrentDotLocation.StartsNewScanlineInVBlank:
            case CurrentDotLocation.StartsPostRender:
                _cycle = 0;
                _scanline++;
                break;

            case CurrentDotLocation.StartsPreRender:
                _cycle = 0;
                _scanline++;
                PpuStatus.VBlankFlag = false;
                break;

            case CurrentDotLocation.StartsFirstDisplayableScanLine:
                _cycle = 0;
                _scanline = 0;
                _hasNmiTriggered = false;
                _pixelIndex = 0;
                RebuildScanLineRenderInfo();
                HandleDisplayablePixelLogic();
                break;

            case CurrentDotLocation.StartsVBlank:
                _cycle = 0;
                _scanline++;
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
        var bgColor = DrawBackgroundPixel2();

        _framebuffer[_pixelIndex] = bgColor ?? new RgbColor(0, 0, 0);
    }

    private RgbColor? DrawBackgroundPixel2()
    {
        if (!PpuMask.EnableBackgroundRendering ||
            (_cycle < 8 && !PpuMask.ShowBackgroundInLeftmost8PixelsOfScreen))
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
            3 => paletteTable[paletteStart + 3],
            _ => throw new ArgumentOutOfRangeException(value.ToString())
        };

        // Sprite 0 hit flag should only occur if the background is opaque. However, there's a weird issue
        // in SMB where after 2 screens worth of space the name table flips back and forth incorrectly
        // between 2000 and 2400, which causes sprite 0 hit flag to never be set (because the background there
        // is transparent) and thus it locks up.
        if (/*value != 0 && */ !PpuStatus.Sprite0HitFlag)
        {
            var targetX = _cycle;
            var targetY = _scanline;

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

        var bank = PpuCtrl.SpritePatternTableAddressFor8X8 switch
        {
            PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex0000 => 0x0000,
            PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex1000 => 0x1000,
            _ => throw new NotSupportedException(PpuCtrl.SpritePatternTableAddressFor8X8.ToString()),
        };

        var tileStart = bank + tileIndex * 16;
        var tileData = _memory.AsMemory(tileStart, 16);

        return new SpriteAttributes(spriteIndex, flipVertical, flipHorizontal, tileData, paletteStartOffset);
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
    //
    // private RgbColor? DrawSpritePixel2(RgbColor? bgColor)
    // {
    //     var targetX = _currentScanLineCycle;
    //     var targetY = _currentScanLine;
    //
    //     var bank = PpuCtrl.SpritePatternTableAddressFor8X8 switch
    //     {
    //         PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex0000 => 0x0000,
    //         PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex1000 => 0x1000,
    //         _ => throw new NotSupportedException(PpuCtrl.SpritePatternTableAddressFor8X8.ToString()),
    //     };
    //
    //     if (!PpuMask.EnableSpriteRendering || (targetX < 8 && PpuMask.ShowSpritesInLeftmost8PixelsOfScreen))
    //     {
    //         return null;
    //     }
    //
    //     var paletteTable = _memory.AsSpan()[0x3F00..];
    //     RgbColor? color = null;
    //     foreach (var sprite in _orderedSprites)
    //     {
    //         if (targetX < sprite.StartX || targetX >= sprite.StartX + 8 ||
    //             targetY < sprite.StartY || targetY >= sprite.StartY + 8)
    //         {
    //             continue;
    //         }
    //
    //         var tileIndex = _oamMemory[sprite.Index + 1];
    //         var tileOffset = bank + tileIndex * 16;
    //         var tileData = _memory.AsSpan(tileOffset, 16);
    //
    //         // Calculate which pixel within the tile we need
    //         var pixelX = targetX - sprite.StartX;
    //         var pixelY = targetY - sprite.StartY;
    //
    //         // Account for flipping
    //         var tilePixelX = sprite.FlipHorizontal ? 7 - pixelX : pixelX;
    //         var tilePixelY = sprite.FlipVertical ? 7 - pixelY : pixelY;
    //
    //         // Read the specific pixel from the tile data
    //         var upper = tileData[tilePixelY];
    //         var lower = tileData[tilePixelY + 8];
    //
    //         // Extract the bit at position tilePixelX (where 7 is leftmost, 0 is rightmost)
    //         var bitPosition = 7 - tilePixelX;
    //         var value = (((lower >> bitPosition) & 1) << 1) | ((upper >> bitPosition) & 1);
    //
    //         if (value == 0)
    //         {
    //             continue; // Transparent pixel, check next sprite
    //         }
    //
    //         color = _systemPalette[paletteTable[sprite.PaletteStartOffset + value - 1]];
    //
    //         // Set sprite 0 hit flag
    //         if (sprite.Index == 0 && bgColor != null)
    //         {
    //             PpuStatus.Sprite0HitFlag = true;
    //         }
    //     }
    //
    //     return color;
    // }

    private RgbColor? DrawBackgroundPixel()
    {
        if (!PpuMask.EnableBackgroundRendering ||
            (_cycle < 8 && !PpuMask.ShowBackgroundInLeftmost8PixelsOfScreen))
        {
            return null;
        }

        const int tileSize = 16; // Each tile is 16 bytes

        // Find the scrolled position within the 2x2 name tables
        var scrolledX = (_cycle + _xScrollRegister) % 512;
        var scrolledY = (_scanline + _yScrollRegister) % 480;
        var nameTableAddress = GetNameTable(scrolledX, scrolledY);

        // Calculate the name table byte that's relevant to the current pixel
        var pixelXInNameTable = scrolledX % 256;
        var pixelYInNameTable = scrolledY % 240;
        var tileColumn = pixelXInNameTable / 8;
        var tileRow = pixelYInNameTable / 8;
        var tileByteOffset = tileRow * 32 + tileColumn;

        ushort backgroundTableAddress = PpuCtrl.BackgroundPatternTableAddress switch
        {
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex0000 => 0x0000,
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex1000 => 0x1000,
            _ => throw new NotSupportedException(PpuCtrl.BackgroundPatternTableAddress.ToString()),
        };

        var palette = GetBackgroundPaletteIndexes(tileColumn, tileRow, nameTableAddress);
        var tileIndex = _memory[nameTableAddress + tileByteOffset];
        var tileStart = backgroundTableAddress + tileIndex * tileSize;
        var tileData = _memory[tileStart..(tileStart + tileSize)];

        // Each tile is 8x8 pixels with 2 bits per pixel, but you have one bit in the first
        // set of 8 bytes and the second bit in the second set of 8 bytes. So we need to isolate out the
        // correct upper and lower bit values
        var tileX = pixelXInNameTable % 8;
        var tileY = pixelYInNameTable % 8;

        var plane0 = tileData[tileY] >> (7 - tileX); // MSB is left most pixel
        var plane1 = tileData[tileY + 8] >> (7 - tileX);
        var value = ((1 & plane1) << 1) | (1 & plane0);
        var color = _systemPalette[palette[value]];

        return color;
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

    private RgbColor? DrawSpritePixel(RgbColor? bgColor)
    {
        var targetX = _cycle;
        var targetY = _scanline;

        if (!PpuMask.EnableSpriteRendering || (targetX < 8 && PpuMask.ShowSpritesInLeftmost8PixelsOfScreen))
        {
            return null;
        }

        RgbColor? color = null;
        for (var index = 0; index < 256; index += 4)
        {
            var tileIndex = _oamMemory[index + 1];
            var tileX = _oamMemory[index + 3];
            var tileY = _oamMemory[index];

            // Early exit: check if this sprite could possibly contain the target pixel
            if (targetX < tileX || targetX >= tileX + 8 ||
                targetY < tileY || targetY >= tileY + 8)
            {
                continue;
            }

            var flipVertical = ((_oamMemory[index + 2] >> 7) & 1) == 1;
            var flipHorizontal = ((_oamMemory[index + 2] >> 6) & 1) == 1;
            var paletteIndex = _oamMemory[index + 2] & 0b11;
            var palette = GetSpritePaletteIndexes(paletteIndex);

            var bank = PpuCtrl.SpritePatternTableAddressFor8X8 switch
            {
                PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex0000 => 0x0000,
                PpuCtrl.SpritePatternTableAddressFor8X8Enum.Hex1000 => 0x1000,
                _ => throw new NotSupportedException(PpuCtrl.SpritePatternTableAddressFor8X8.ToString()),
            };

            var tileStart = bank + tileIndex * 16;
            var tileData = _memory.AsSpan(tileStart, 16);

            // Calculate which pixel within the tile we need
            var pixelX = targetX - tileX;
            var pixelY = targetY - tileY;

            // Account for flipping
            var tilePixelX = flipHorizontal ? 7 - pixelX : pixelX;
            var tilePixelY = flipVertical ? 7 - pixelY : pixelY;

            // Read the specific pixel from the tile data
            var upper = tileData[tilePixelY];
            var lower = tileData[tilePixelY + 8];

            // Extract the bit at position tilePixelX (where 7 is leftmost, 0 is rightmost)
            var bitPosition = 7 - tilePixelX;
            var value = (((lower >> bitPosition) & 1) << 1) | ((upper >> bitPosition) & 1);

            if (value == 0)
            {
                continue; // Transparent pixel, check next sprite
            }

            color = value switch
            {
                1 => _systemPalette[palette[1]],
                2 => _systemPalette[palette[2]],
                3 => _systemPalette[palette[3]],
                _ => throw new NotSupportedException(value.ToString()),
            };

            // Set sprite 0 hit flag
            if (index == 0 && bgColor != null)
            {
                PpuStatus.Sprite0HitFlag = true;
            }
        }

        return color;
    }

    private void DrawSprites()
    {
        for (var index = 0; index < 256; index += 4)
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
                    SetPixel(tileX + x, tileY + y, color.Value);
                }
            }
        }
    }

    private byte[] GetBackgroundPaletteIndexes(int tileColumn, int tileRow, ushort nameTableAddress)
    {
        var attributeTableIndex = tileRow / 4 * 8 + tileColumn / 4;
        var attributeTableLocation = nameTableAddress + NameTableSize;
        var attributeByteLocation = attributeTableLocation + attributeTableIndex;
        var attributeByte = _memory[attributeByteLocation];
        var palletIndex = (tileColumn % 4 / 2, tileRow % 4 / 2) switch
        {
            (0, 0) => attributeByte & 0b11,
            (1, 0) => (attributeByte >> 2) & 0b11,
            (0, 1) => (attributeByte >> 4) & 0b11,
            (1, 1) => (attributeByte >> 6) & 0b11,
            _ => throw new NotSupportedException(),
        };

        var paletteStart = 1 + palletIndex * 4;
        var paletteTable = _memory.AsSpan()[0x3F00..];
        return
        [
            paletteTable[0],
            paletteTable[paletteStart],
            paletteTable[paletteStart + 1],
            paletteTable[paletteStart + 2],
        ];
    }

    private byte[] GetSpritePaletteIndexes(int paletteIndex)
    {
        var start = 0x11 + (paletteIndex * 4);
        var paletteTable = _memory.AsSpan()[0x3F00..];
        return
        [
            0,
            paletteTable[start],
            paletteTable[start + 1],
            paletteTable[start + 2],
        ];
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
        var isInPreRender = _scanline == TotalScanLines - 1;
        var isInVisibleRegion = _scanline < DisplayableScanLines;
        var isWithinCycleBounds = _cycle is >= 257 and <= 320;

        if ((isInPreRender || isInVisibleRegion) && isWithinCycleBounds)
        {
            _oamAddrRegister = 0;
        }
    }

    private CurrentDotLocation GetCurrentDotLocation()
    {
        // NOTE: Assumes cycle count has already been incremented by one, but nothing else has been incremented
        if (_cycle > PpuCyclesPerScanline)
        {
            var message = $"Expected cycle count to never get past {PpuCyclesPerScanline}, but it was {_cycle}";
            throw new InvalidOperationException(message);
        }

        if (_cycle == PpuCyclesPerScanline)
        {
            // Starting a new scanline. Scan line count hasn't been incremented yet
            if (_scanline < DisplayableScanLines - 1)
            {
                return CurrentDotLocation.StartsNewDisplayableScanline;
            }

            if (_scanline == DisplayableScanLines - 1)
            {
                return CurrentDotLocation.StartsPostRender;
            }

            if (_scanline == DisplayableScanLines + PostRenderBlankingLines - 1)
            {
                return CurrentDotLocation.StartsVBlank;
            }

            if (_scanline == TotalScanLines - PreRenderBlankingLines - 1)
            {
                return CurrentDotLocation.StartsPreRender;
            }

            if (_scanline == TotalScanLines - 1)
            {
                return CurrentDotLocation.StartsFirstDisplayableScanLine;
            }

            return CurrentDotLocation.StartsNewScanlineInVBlank;
        }

        // Within a scanline
        if (_scanline == DisplayableScanLines)
        {
            return CurrentDotLocation.InPostRender;
        }

        if (_scanline == TotalScanLines - PreRenderBlankingLines - 1)
        {
            return CurrentDotLocation.InPreRender;
        }

        if (_scanline > DisplayableScanLines)
        {
            return CurrentDotLocation.InVBlank;
        }

        if (_cycle < DisplayableWidth)
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
        var scrolledY = (_scanline + _yScrollRegister) % 480;
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
        int Index,
        bool FlipVertical,
        bool FlipHorizontal,
        Memory<byte> TileData,
        int PaletteStartOffset);

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

    private class VramRegister
    {
        private const int FineYMask = 0b0_111_00_00000_00000;
        private const int NameTableMask = 0b0_000_11_00000_00000;
        private const int CoarseYMask = 0b0_000_00_11111_00000;
        private const int CoarseXMask = 0b0_000_00_00000_11111;

        private const int FineYShift = 12;
        private const int NameTableShift = 10;
        private const int CoarseYShift = 5;
        private const int CoarseXShift = 0;

        public ushort RawValue;

        public byte CoarseXScroll
        {
            get => (byte)((RawValue & CoarseXMask) >> CoarseXShift);
            set => RawValue = (ushort)((RawValue & ~CoarseXMask ) | (value << CoarseXShift));
        }

        public byte CoarseYScroll
        {
            get => (byte)((RawValue & CoarseYMask) >> CoarseYShift);
            set => RawValue = (ushort)((RawValue & ~CoarseYMask) | (value << CoarseYShift));
        }

        public byte NameTableSelect
        {
            get => (byte)((RawValue & NameTableMask) >> NameTableShift);
            set => RawValue = (ushort)((RawValue & ~NameTableMask) | (value << NameTableShift));
        }

        public byte FineYScroll
        {
            get => (byte)((RawValue & FineYMask) >> FineYShift);
            set => RawValue = (ushort)((RawValue & ~FineYMask) | (value << FineYShift));
        }

        public void IncrementX()
        {
            // The coarse X component of v needs to be incremented when the next tile is reached. Bits 0-4
            // are incremented, with overflow toggling bit 10. This means that bits 0-4 count from 0 to 31
            // across a single nametable, and bit 10 selects the current nametable horizontally.
            if ((RawValue & 0x001F) == 31)
            {
                RawValue &= 0xFFE0;
                RawValue ^= 0x0400; // switch horizontal name table
            }
            else
            {
                RawValue += 1;
            }
        }

        public void IncrementY()
        {
            // If rendering is enabled, fine Y is incremented at dot 256 of each scanline, overflowing to
            // coarse Y, and finally adjusted to wrap among the nametables vertically. Bits 12-14 are fine Y.
            // Bits 5-9 are coarse Y. Bit 11 selects the vertical nametable.

            if ((RawValue & 0x7000) != 0x7000) // If fine Y < 7
            {
                RawValue += 0x1000;
            }
            else
            {
                RawValue &= 0x8FFF; // Fine y = 0;
                var y = (RawValue & 0x03E0) >> 5; // coarse y value
                if (y == 29) // If last row of tiles in a nametable
                {
                    y = 0;
                    RawValue ^= 0x0800; // Switch vertical nametable
                }
                else if (y == 31)
                {
                    y = 0; // coarse y = 0, nametable not switched
                }
                else
                {
                    y += 1; // increment coarse y
                }

                RawValue = (ushort)((RawValue & 0xFC1F) | (y << 5));
            }
        }
    }
}