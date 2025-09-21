namespace Dotnet6502.Nes;

/// <summary>
/// Implementation of the NES PPU
/// </summary>
public class Ppu
{
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

    private int _currentCycle;
    private int _currentScanLine;
    private bool _hasNmiTriggered;

    /// <summary>
    /// Executes the next Ppu step based on the number of CPU cycles about to be executed.
    /// </summary>
    /// <returns>True if this execution triggered NMI (NMI enable & vblank PPU Status)</returns>
    public bool RunNextStep(int cpuCycleCount)
    {
        var ppuCycles = cpuCycleCount * PpuCyclesPerCpuCycle;

        for (var x = 0; x < ppuCycles; x++)
        {
            _currentCycle++;
            if (_currentCycle >= PpuCyclesPerScanline)
            {
                _currentCycle = 0;
                _currentScanLine++;

                if (_currentScanLine == DisplayableScanLines + PostRenderBlankingLines)
                {
                    _ppuStatus.VBlankFlag = true;
                }
                else if (_currentScanLine == TotalScanLines)
                {
                    _ppuStatus.VBlankFlag = false;
                    _hasNmiTriggered = false;
                }

                DrawNextPixel();
            }
        }

        var triggerNmi = _ppuCtrl.NmiEnable == PpuCtrl.NmiEnableValue.On && _ppuStatus.VBlankFlag && !_hasNmiTriggered;
        if (triggerNmi)
        {
            _hasNmiTriggered = true;
        }

        return triggerNmi;
    }

    public void Write(ushort address, byte value)
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

    public byte Read(ushort address)
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
}