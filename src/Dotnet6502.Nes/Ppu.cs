using NESDecompiler.Core.ROM;

namespace Dotnet6502.Nes;

/// <summary>
/// Implementation of the NES PPU
/// </summary>
public class Ppu
{
    private readonly record struct Rectangle(int X1, int Y1, int X2, int Y2);

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
    private int _pixelIndex;
    private int _currentScanLineCycle;
    private int _currentScanLine; // zero based index of what scan line we are currently at
    private bool _hasNmiTriggered; // Has NMI been marked as to be triggered this frame

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
                    _memory[mirroredAddress] = value;
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
        var x = _oamMemory[3];
        var y = _oamMemory[0];

        var isSprite0Hit = y == _currentScanLine &&
                           x <= _currentScanLineCycle &&
                           PpuMask.ShowSpritesInLeftmost8PixelsOfScreen;

        if (isSprite0Hit)
        {
            PpuStatus.Sprite0HitFlag = true;
        }

        DrawNextPixel();
    }

    private void DrawNextPixel()
    {
        var color = DrawBackgroundPixel();

        _framebuffer[_pixelIndex] = color;
    }

    private RgbColor DrawBackgroundPixel()
    {
        const int tileSize = 16; // Each tile is 16 bytes

        // Find the scrolled position within the 2x2 name tables
        var scrolledX = (_currentScanLineCycle + _xScrollRegister) % 512;
        var scrolledY = (_currentScanLine + _yScrollRegister) % 480;
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

        var palette = GetBackgroundPaletteIndexes(tileColumn, tileRow);
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

    private byte GetTileIndex(int offset)
    {
        ushort nameTableAddress = PpuCtrl.BaseNameTableAddress switch
        {
            PpuCtrl.BaseNameTableAddressValue.Hex2000 => 0x2000,
            PpuCtrl.BaseNameTableAddressValue.Hex2400 => 0x2400,
            PpuCtrl.BaseNameTableAddressValue.Hex2800 => 0x2800,
            PpuCtrl.BaseNameTableAddressValue.Hex2C00 => 0x2C00,
            _ => throw new NotSupportedException(PpuCtrl.BaseNameTableAddress.ToString()),
        };

        return _memory[nameTableAddress + offset];
    }

    private void RenderFrame()
    {
        // RenderFullBackground();
        RenderSprites();

        _nesDisplay.RenderFrame(_framebuffer);

        for (var x = 0; x < _framebuffer.Length; x++)
        {
            _framebuffer[x] = new RgbColor(0, 0, 0);
        }
    }

    private void RenderSprites()
    {
        for (var index = 0; index < 256; index += 4)
        {
            var tileIndex = _oamMemory[index + 1];
            var tileX = _oamMemory[index + 3];
            var tileY = _oamMemory[index];

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

            for (var y = 0; y < 8; y++)
            {
                var upper = tileData[y];
                var lower = tileData[y + 8];
                for (var x = 7; x >= 0; x--)
                {
                    var value = (1 & lower) << 1 | (1 & upper);
                    upper = (byte)(upper >> 1);
                    lower = (byte)(lower >> 1);

                    if (value == 0)
                    {
                        continue; // Skip the pixel, acts as transparent
                    }

                    var color = value switch
                    {
                        1 => _systemPalette[palette[1]],
                        2 => _systemPalette[palette[2]],
                        3 => _systemPalette[palette[3]],
                        _ => throw new NotSupportedException(value.ToString()),
                    };

                    var renderX = flipHorizontal ? tileX + 7 - x : tileX + x;
                    var renderY = flipVertical ? tileY + 7 - y : tileY + y;
                    SetPixel(renderX, renderY, color);
                }
            }
        }
    }

    private void RenderFullBackground()
    {
        ushort nameTableAddress = PpuCtrl.BaseNameTableAddress switch
        {
            PpuCtrl.BaseNameTableAddressValue.Hex2000 => 0x2000,
            PpuCtrl.BaseNameTableAddressValue.Hex2400 => 0x2400,
            PpuCtrl.BaseNameTableAddressValue.Hex2800 => 0x2800,
            PpuCtrl.BaseNameTableAddressValue.Hex2C00 => 0x2C00,
            _ => throw new NotSupportedException(PpuCtrl.BaseNameTableAddress.ToString()),
        };

        ushort mainNameTable, secondNameTable;
        if (_mirroringType == MirroringType.FourScreen)
        {
            throw new NotSupportedException(_mirroringType.ToString());
        }

        if ((_mirroringType == MirroringType.Horizontal && nameTableAddress is 0x2000 or 0x2400) ||
            (_mirroringType == MirroringType.Vertical && nameTableAddress is 0x2000 or 0x2800))
        {
            mainNameTable = 0x2000;
            secondNameTable = 0x2400;
        }
        else
        {
            mainNameTable = 0x2400;
            secondNameTable = 0x2000;
        }

        RenderNameTable(mainNameTable,
            new Rectangle(_xScrollRegister, _yScrollRegister, 256, 240),
            -_xScrollRegister,
            -_yScrollRegister);

        if (_xScrollRegister > 0)
        {
            RenderNameTable(secondNameTable,
                new Rectangle(0, 0, _xScrollRegister, 240),
                (256 - _xScrollRegister),
                0);
        }
        else if (_yScrollRegister > 0)
        {
            RenderNameTable(secondNameTable,
                new Rectangle(0, 0, 256, _yScrollRegister),
                0,
                (240 - _yScrollRegister));
        }
    }

    private void RenderNameTable(
        ushort nameTableAddress,
        Rectangle viewPort,
        int shiftX,
        int shiftY)
    {
        ushort backgroundTableAddress = PpuCtrl.BackgroundPatternTableAddress switch
        {
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex0000 => 0x0000,
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex1000 => 0x1000,
            _ => throw new NotSupportedException(PpuCtrl.BackgroundPatternTableAddress.ToString()),
        };

        var nameTableBytes = _memory.AsSpan().Slice(nameTableAddress, 960);
        for (var i = 0; i < nameTableBytes.Length; i++)
        {
            var tileIndex = nameTableBytes[i];
            var tileX = i % 32;
            var tileY = i / 32;
            var palette = GetBackgroundPaletteIndexes(tileX, tileY);
            var tileStart = backgroundTableAddress + tileIndex * 16;
            var tileEnd = tileStart + 16;
            var tileData = _memory[tileStart..tileEnd];

            for (var y = 0; y < 8; y++)
            {
                var upper = tileData[y];
                var lower = tileData[y + 8];

                for (var x = 7; x >= 0; x--)
                {
                    var value = ((1 & lower) << 1) | (1 & upper);
                    upper = (byte)(upper >> 1);
                    lower = (byte)(lower >> 1);

                    var color = value switch
                    {
                        0 => _systemPalette[palette[0]],
                        1 => _systemPalette[palette[1]],
                        2 => _systemPalette[palette[2]],
                        3 => _systemPalette[palette[3]],
                        _ => throw new NotSupportedException(value.ToString()),
                    };

                    var finalX = tileX * 8 + x;
                    var finalY = tileY * 8 + y;

                    var isInFrame = finalX >= viewPort.X1 &&
                                    finalX < viewPort.X2 &&
                                    finalY >= viewPort.Y1 &&
                                    finalY < viewPort.Y2;

                    if (isInFrame)
                    {
                        SetPixel(finalX + shiftX, finalY + shiftY, color);
                    }
                }
            }
        }
    }

    private byte[] GetBackgroundPaletteIndexes(int tileColumn, int tileRow)
    {
        ushort nameTableAddress = PpuCtrl.BaseNameTableAddress switch
        {
            PpuCtrl.BaseNameTableAddressValue.Hex2000 => 0x2000,
            PpuCtrl.BaseNameTableAddressValue.Hex2400 => 0x2400,
            PpuCtrl.BaseNameTableAddressValue.Hex2800 => 0x2800,
            PpuCtrl.BaseNameTableAddressValue.Hex2C00 => 0x2C00,
            _ => throw new NotSupportedException(PpuCtrl.BaseNameTableAddress.ToString()),
        };

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

        if (_currentScanLine == DisplayableScanLines + PostRenderBlankingLines + PreRenderBlankingLines - 1)
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
}