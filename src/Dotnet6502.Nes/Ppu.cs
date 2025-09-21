namespace Dotnet6502.Nes;

/// <summary>
/// Implementation of the NES PPU
/// </summary>
public class Ppu
{
    private enum CurrentDotLocation
    {
        InDisplayableArea, InHBlank, InPostRender, StartsNewDisplayableScanline, StartsPostRender, StartsVBlank,
        InVBlank, EndsVBlank,
    }
    
    private const int PpuCyclesPerScanline = 341;
    private const int DisplayableScanLines = 240;
    private const int PostRenderBlankingLines = 1;
    private const int VBlankLinesPostNmi = 20;
    private const int TotalScanLines = DisplayableScanLines + PostRenderBlankingLines + VBlankLinesPostNmi;
    private const int HBlankCycles = 61;
    private const int PpuCyclesPerCpuCycle = 3;

    private const int DisplayableWidth = PpuCyclesPerScanline - HBlankCycles;

    // Registers
    private readonly PpuCtrl _ppuCtrl;
    private readonly PpuStatus _ppuStatus;
    private readonly PpuMask _ppuMask;
    private byte _oamDmaRegister;
    private byte _oamAddrRegister;
    private byte _oamDataRegister;
    private byte _xScrollRegister, _yScrollRegister;
    private ushort _ppuAddr;
    private bool _wRegister;

    private readonly RgbColor[] _framebuffer = new RgbColor[DisplayableWidth * DisplayableScanLines];
    private int _pixelIndex;
    private int _currentScanLineCycle;
    private int _currentScanLine; // zero based index of what scan line we are currently at
    private bool _hasNmiTriggered; // Has NMI been marked as to be triggered this frame

    public Ppu()
    {
        _ppuCtrl = new PpuCtrl();
        _ppuStatus = new PpuStatus();
        _ppuMask = new PpuMask();
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
                    break; // Nothing to do
                
                case CurrentDotLocation.StartsNewDisplayableScanline:
                    _currentScanLineCycle = 0;
                    _currentScanLine++;
                    _pixelIndex++;
                    DrawNextPixel();
                    break;

                case CurrentDotLocation.StartsPostRender:
                    _currentScanLineCycle = 0;
                    _currentScanLine++;
                    break;

                case CurrentDotLocation.EndsVBlank:
                    // New frame
                    _currentScanLineCycle = 0;
                    _currentScanLine = 0;
                    _ppuStatus.VBlankFlag = false;
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
        if (address == 0x4014)
        {
            _oamDmaRegister = value;
            return;
        }

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
                _oamDataRegister = value;
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
                    _ppuAddr = (ushort)((_ppuAddr & 0x00FF) | (value << 8));
                }

                _wRegister = !_wRegister;
                break;

            case 7:
                throw new NotImplementedException();
                break;
        }
    }

    /// <summary>
    /// Process a request from the CPU to read from a PPU owned memory address
    /// </summary>
    public byte ProcessMemoryRead(ushort address)
    {
        if (address == 0x4014)
        {
            return _oamDmaRegister;
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
                return result;

            case 3:
                return 0; // OamAddr not readable

            case 4:
                return _oamDataRegister;

            case 5:
                return 0; // PPUSCROLL not readable

            case 6:
                return 0; // PPUADDR not readable

            case 7:
                throw new NotImplementedException();

            default:
                throw new NotSupportedException(byteNumber.ToString());
        }
    }

    private void DrawNextPixel()
    {
        throw new NotImplementedException();
    }

    private void RenderFrame()
    {
        throw new NotImplementedException();
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

            if (_currentScanLine == TotalScanLines - 1)
            {
                return CurrentDotLocation.EndsVBlank;
            }

            return CurrentDotLocation.InVBlank;
        }

        // Within a scanline
        if (_currentScanLine == DisplayableScanLines)
        {
            return CurrentDotLocation.InPostRender;
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