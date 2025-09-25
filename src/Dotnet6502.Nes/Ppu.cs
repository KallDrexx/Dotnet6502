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

    // Registers
    private readonly PpuCtrl _ppuCtrl;
    private readonly PpuStatus _ppuStatus;
    private readonly PpuMask _ppuMask;
    private byte _oamAddrRegister;
    private byte _xScrollRegister, _yScrollRegister;
    private ushort _ppuAddr;
    private bool _wRegister;
    private byte _vRegister;
    private byte _tRegister;
    private byte _xRegister;

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

        if (chrRomData.Length != 0x2000)
        {
            var message = $"Expected chrRomData to be 0x2000 bytes, but was {chrRomData.Length:X4}";
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
                break;

            case 7:
                _memory[_ppuAddr] = value;
                if (_ppuCtrl.VRamAddressIncrement == PpuCtrl.VRamAddressIncrementValue.Add1Across)
                {
                    _ppuAddr += 1;
                }
                else
                {
                    _ppuAddr += 32;
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
                var value = _memory[_ppuAddr];
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

    private void DrawNextPixel()
    {
    }

    private void RenderFrame()
    {
        _nesDisplay.RenderFrame(_framebuffer);

        for (var x = 0; x < _framebuffer.Length; x++)
        {
            _framebuffer[x] = new RgbColor(0, 0, 0);
        }
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