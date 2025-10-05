namespace Dotnet6502.Nes;

public class DebugWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly DebugLogSections _sections;
    private readonly Ppu _ppu;

    public DebugWriter(FileInfo outputLog, DebugLogSections sections, Ppu ppu)
    {
        _sections = sections;
        _ppu = ppu;

        try
        {
            File.Delete(outputLog.FullName);
        }
        catch
        {
            // Ignore deletion errors, file may not exist
        }

        _writer = File.CreateText(outputLog.FullName);
        _writer.AutoFlush = true;

        Console.WriteLine($"Writing debug log to: {outputLog.FullName}");
    }

    public void Log(bool isInNmi, NesHal hal, string info, bool includeState)
    {
        var shouldLog = (isInNmi && _sections != DebugLogSections.OnlyNonNmi) ||
                        (!isInNmi && _sections != DebugLogSections.OnlyNmi);

        if (!shouldLog)
        {
            return;
        }

        _writer.Write($"{info}");
        if (includeState)
        {
            _writer.Write(
                $" - A:{hal.ARegister:X2} X:{hal.XRegister:X2} Y:{hal.YRegister:X2} P:{hal.ProcessorStatus:X2} ");
            _writer.Write($"SP:{hal.StackPointer:X2} ");
            _writer.Write($"PPUCTL:{_ppu.PpuCtrl.ToByte():X2} PPUSTATUS:{_ppu.PpuStatus.ToByte():X2} ");
            _writer.Write($"PPUADDR:{_ppu.PpuAddr:X4}");
        }

        _writer.WriteLine();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}