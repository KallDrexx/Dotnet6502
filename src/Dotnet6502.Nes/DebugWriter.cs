namespace Dotnet6502.Nes;

public class DebugWriter : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly Ppu _ppu;

    public DebugWriter(FileInfo outputLog, Ppu ppu)
    {
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

    public void Log(NesHal hal, string info, bool includeState)
    {
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