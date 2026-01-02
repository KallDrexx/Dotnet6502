using Dotnet6502.C64.Emulation;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// Emulates the NTSC VIC-II display system
/// </summary>
public class Vic2
{
    private const int DotsPerCpuCycle = 8;
    private const int DotsPerScanline = 520;
    private const int VisibleDotsPerScanline = DotsPerScanline - 56;
    private const int TotalScanlines = 263;
    private const int VisibleScanlines = 235;
    private const int NonVisibleScanlines = TotalScanlines - VisibleScanlines;

    private readonly IC64Display _c64Display;
    private int _dotCount;
    private int _scanlineCount;

    public Vic2(IC64Display c64Display)
    {
        _c64Display = c64Display;
    }

    public void RunSingleCpuCycle()
    {
        _dotCount += DotsPerCpuCycle;
        if (_dotCount >= DotsPerScanline)
        {
            _scanlineCount++;
            _dotCount = 0;
            if (_scanlineCount > TotalScanlines)
            {
                _scanlineCount = 0;
                _c64Display.RenderFrame(new RgbColor[VisibleDotsPerScanline * VisibleScanlines]);
            }
        }
    }
}