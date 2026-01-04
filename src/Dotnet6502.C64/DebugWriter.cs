using Dotnet6502.C64.Hardware;

namespace Dotnet6502.C64;

public class DebugWriter : IDisposable
{
    private record SystemState(byte A, byte X, byte Y, byte P, byte S);

    private readonly StreamWriter _writer;
    private SystemState? _previousState;

    public DebugWriter(FileInfo outputLog)
    {
        try
        {
            File.Delete(outputLog.FullName);
        }
        catch
        {
            // Ignore deletion errors, file may not exist
        }

        _writer = File.CreateText(outputLog.FullName);
        _writer.AutoFlush = false;

        Console.WriteLine($"Writing debug log to: {outputLog.FullName}");
    }

    public void Log(C64Hal hal, string info, bool includeState)
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
        _writer.Flush();
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

    private SystemState GetState(C64Hal hal)
    {
        return new SystemState(
            hal.ARegister,
            hal.XRegister,
            hal.YRegister,
            hal.ProcessorStatus,
            hal.StackPointer);
    }
}