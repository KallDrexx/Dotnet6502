namespace Dotnet6502.Nes;

/// <summary>
/// Implementation of the NES PPU
/// </summary>
public class Ppu
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
    private readonly PpuCtrl _ppuCtrl;
    private readonly PpuStatus _ppuStatus;
    private readonly PpuMask _ppuMask;
    private byte _oamAddrRegister;
    private byte _xScrollRegister, _yScrollRegister;
    private ushort _ppuAddr;
    private bool _wRegister;
    private ushort _vRegister;
    private byte _tRegister;
    private byte _xRegister;
    private byte _readBuffer;

    private readonly INesDisplay _nesDisplay;
    private readonly RgbColor[] _framebuffer = new RgbColor[DisplayableWidth * DisplayableScanLines];
    private readonly byte[] _memory = new byte[0x4000];
    private readonly byte[] _oamMemory = new byte[0x100];
    private int _pixelIndex;
    private int _currentScanLineCycle;
    private int _currentScanLine; // zero based index of what scan line we are currently at
    private bool _hasNmiTriggered; // Has NMI been marked as to be triggered this frame

    public Ppu(byte[] chrRomData, INesDisplay nesDisplay)
    {
        _nesDisplay = nesDisplay;
        _ppuCtrl = new PpuCtrl();
        _ppuStatus = new PpuStatus();
        _ppuMask = new PpuMask();

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

        var triggerNmi = _ppuCtrl.NmiEnable == PpuCtrl.NmiEnableValue.On && _ppuStatus.VBlankFlag && !_hasNmiTriggered;
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
                _ppuCtrl.UpdateFromByte(value);
                break;

            case 1:
                _ppuMask.UpdateFromByte(value);
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
                    _ppuAddr = (ushort)((_ppuAddr & 0xFF00) | value);
                }
                else
                {
                    // PPUADDR is only 14 bits, so clear out the two high bits
                    var maskedValue = value & 0x3F;
                    _ppuAddr = (ushort)((_ppuAddr & 0x00FF) | (maskedValue << 8));
                }

                _wRegister = !_wRegister;
                _vRegister = _ppuAddr;

                // Prime the read buffer
                _readBuffer = _memory[_ppuAddr];
                break;

            case 7:
                if (_ppuAddr >= 0x3F00)
                {
                    var mirroredAddress = GetPaletteMemoryLocation(_ppuAddr);
                    _memory[mirroredAddress] = value;
                }
                else
                {
                    _memory[_ppuAddr] = value;
                }

                if (_ppuCtrl.VRamAddressIncrement == PpuCtrl.VRamAddressIncrementValue.Add1Across)
                {
                    _ppuAddr += 1;
                }
                else
                {
                    _ppuAddr += 32;
                }

                _vRegister = _ppuAddr;

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
                var result = _ppuStatus.ToByte();
                _ppuStatus.VBlankFlag = false; // Clear vblank on read
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
                if (_ppuAddr < 0x3F00)
                {
                    value = _readBuffer;
                    _readBuffer = _memory[_ppuAddr];
                }
                else
                {
                    var mirroredAddress = GetPaletteMemoryLocation(_ppuAddr);
                    value = _memory[mirroredAddress];
                }

                if (_ppuCtrl.VRamAddressIncrement == PpuCtrl.VRamAddressIncrementValue.Add1Across)
                {
                    _ppuAddr += 1;
                }
                else
                {
                    _ppuAddr += 32;
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
                DrawNextPixel();
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
                DrawNextPixel();
                break;

            case CurrentDotLocation.StartsNewScanlineInVBlank:
            case CurrentDotLocation.StartsPostRender:
                _currentScanLineCycle = 0;
                _currentScanLine++;
                break;

            case CurrentDotLocation.StartsPreRender:
                _currentScanLineCycle = 0;
                _currentScanLine++;
                _ppuStatus.VBlankFlag = false;
                break;

            case CurrentDotLocation.StartsFirstDisplayableScanLine:
                _currentScanLineCycle = 0;
                _currentScanLine = 0;
                _hasNmiTriggered = false;
                _pixelIndex = 0;
                DrawNextPixel();
                break;

            case CurrentDotLocation.StartsVBlank:
                _currentScanLineCycle = 0;
                _currentScanLine++;
                _ppuStatus.VBlankFlag = true;
                RenderFrame();
                break;

            default:
                throw new NotSupportedException(currentLocation.ToString());
        }

        ResetOamData();
    }

    private void DrawNextPixel()
    {
        // TODO: Only full render at the end of the frame supported atm
    }

    private void RenderFrame()
    {
        ushort nameTableAddress = _ppuCtrl.BaseNameTableAddress switch
        {
            PpuCtrl.BaseNameTableAddressValue.Hex2000 => 0x2000,
            PpuCtrl.BaseNameTableAddressValue.Hex2400 => 0x2400,
            PpuCtrl.BaseNameTableAddressValue.Hex2800 => 0x2800,
            PpuCtrl.BaseNameTableAddressValue.Hex2C00 => 0x2C00,
            _ => throw new NotSupportedException(_ppuCtrl.BaseNameTableAddress.ToString()),
        };

        ushort backgroundTableAddress = _ppuCtrl.BackgroundPatternTableAddress switch
        {
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex0000 => 0x0000,
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex1000 => 0x1000,
            _ => throw new NotSupportedException(_ppuCtrl.BackgroundPatternTableAddress.ToString()),
        };

        var nameTableBytes = _memory.AsSpan().Slice(nameTableAddress, 960);
        for (var i = 0; i < nameTableBytes.Length; i++)
        {
            var tileIndex = nameTableBytes[i];
            var tilex = i % 32;
            var tiley = i / 32;
            var palette = GetBackgroundPaletteIndexes(tilex, tiley);
            // var palette = new byte[] { 0x01, 0x23, 0x27, 0x30 };
            ShowTile(backgroundTableAddress, tileIndex, tilex * 8, tiley * 8, palette);
        }
        // RenderPatternTableToFrameBuffer();

        _nesDisplay.RenderFrame(_framebuffer);

        for (var x = 0; x < _framebuffer.Length; x++)
        {
            _framebuffer[x] = new RgbColor(0, 0, 0);
        }
    }

    private void RenderPatternTableToFrameBuffer()
    {
        ushort backgroundTableAddress = _ppuCtrl.BackgroundPatternTableAddress switch
        {
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex0000 => 0x0000,
            PpuCtrl.BackgroundPatternTableAddressEnum.Hex1000 => 0x1000,
            _ => throw new NotSupportedException(_ppuCtrl.BackgroundPatternTableAddress.ToString()),
        };

        var tileX = 2;
        var tileY = 2;
        for (ushort tileNum = 0; tileNum < 255; tileNum++)
        {
            if (tileNum != 0 && tileNum % 16 == 0)
            {
                tileY += 10;
                tileX = 2;
            }

            var palette = new byte[] { 0x01, 0x23, 0x27, 0x30 };
            ShowTile(backgroundTableAddress, tileNum, tileX, tileY, palette);

            tileX += 10;
        }
    }

    private void ShowTile(ushort bankAddress, ushort tileNumber, int startX, int startY, byte[] palette)
    {
        var tileStart = bankAddress + tileNumber * 16;
        var tileEnd = bankAddress + tileNumber * 16 + 16;
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

                SetPixel(x + startX, y + startY, color);
            }
        }
    }

    private byte[] GetBackgroundPaletteIndexes(int tileColumn, int tileRow)
    {
        ushort nameTableAddress = _ppuCtrl.BaseNameTableAddress switch
        {
            PpuCtrl.BaseNameTableAddressValue.Hex2000 => 0x2000,
            PpuCtrl.BaseNameTableAddressValue.Hex2400 => 0x2400,
            PpuCtrl.BaseNameTableAddressValue.Hex2800 => 0x2800,
            PpuCtrl.BaseNameTableAddressValue.Hex2C00 => 0x2C00,
            _ => throw new NotSupportedException(_ppuCtrl.BaseNameTableAddress.ToString()),
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

    private void SetPixel(int x, int y, RgbColor color)
    {
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