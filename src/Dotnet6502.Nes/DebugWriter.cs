namespace Dotnet6502.Nes;

public class DebugWriter : IDisposable
{
    private record SystemState(byte A, byte X, byte Y, byte P, byte S, byte PpuCtl, byte PpuStatus, ushort PpuAddr);

    private readonly StreamWriter _writer;
    private readonly Ppu _ppu;
    private SystemState? _previousState;

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
            var state = GetState(hal);
            _writer.Write(" - ");
            WriteState(state, _previousState);

            _previousState = state;
        }

        _writer.WriteLine();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    private void WriteState(SystemState state, SystemState? previousState)
    {
        _writer.Write(" A:");
        WriteByte(state.A, previousState?.A);
        _writer.Write(" X:");
        WriteByte(state.X, previousState?.X);
        _writer.Write(" Y:");
        WriteByte(state.Y, previousState?.Y);
        _writer.Write(" SP:");
        WriteByte(state.S, previousState?.S);
        _writer.Write(" P:");
        WriteByte(state.P, previousState?.P);
        _writer.Write(" PpuCtl:");
        WriteByte(state.PpuCtl, previousState?.PpuCtl);
        _writer.Write(" PpuStatus:");
        WriteByte(state.PpuStatus, previousState?.PpuStatus);
    }

    private void WriteByte(byte current, byte? previous)
    {
        if (previous == null || current == previous)
        {
            _writer.Write(current.ToString("X2"));
        }
        else
        {
            _writer.Write($"*{current:X2}*");
        }
    }

    private void WriteUshort(ushort current, ushort? previous)
    {
        if (previous == null || current == previous)
        {
            _writer.Write(current.ToString("X4"));
        }
        else
        {
            _writer.Write($"*{current:X4}*");
        }
    }

    private SystemState GetState(NesHal hal)
    {
        return new SystemState(
            hal.ARegister,
            hal.XRegister,
            hal.YRegister,
            hal.ProcessorStatus,
            hal.StackPointer,
            _ppu.PpuCtrl.ToByte(),
            _ppu.PpuStatus.ToByte(),
            _ppu.PpuAddr);
    }
}