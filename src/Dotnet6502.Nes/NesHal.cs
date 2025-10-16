using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Nes;

public class NesHal : Base6502Hal
{
    // ReSharper disable once NotAccessedPositionalProperty.Local
    private readonly record struct MemWriteValue(ushort Address, byte Value, int ScanLine);

    private readonly Ppu _ppu;
    private readonly CancellationToken _cancellationToken;
    private readonly DebugWriter? _debugWriter;
    private readonly Queue<string> _lastInstructions = new();

    // ReSharper disable once CollectionNeverQueried.Local
    private readonly Queue<MemWriteValue> _currentFrameWrittenMemory = new();
    private readonly bool _debugModeEnabled;
    private bool _nmiTriggered;

    public NesHal(NesMemory memory, Ppu ppu, DebugWriter? debugWriter, bool debugModeEnabled, CancellationToken cancellationToken)
        : base(memory)
    {
        _ppu = ppu;
        _cancellationToken = cancellationToken;
        _debugWriter = debugWriter;
        _debugModeEnabled = debugModeEnabled;
    }

    /// <summary>
    /// Increments the CPU cycle count in preparation for an instruction.
    /// </summary>
    public void IncrementCpuCycleCount(int count)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("Cancellation requested");
        }

        _nmiTriggered = _ppu.RunNextStep(count);
    }

    public override byte PopFromStack()
    {
        var value = base.PopFromStack();
        _debugWriter?.Log(this, $"Popped 0x{value:X2} from stack", true);

        return value;
    }

    public override void PushToStack(byte value)
    {
        base.PushToStack(value);
        _debugWriter?.Log(this, $"Pushed 0x{value:X2} to stack", true);
    }

    public override void WriteMemory(ushort address, byte value)
    {
        if (_debugModeEnabled)
        {
            _currentFrameWrittenMemory.Enqueue(new MemWriteValue(address, value, _ppu.CurrentScanLine));
        }

        base.WriteMemory(address, value);
    }

    public override void DebugHook(string info)
    {
        _debugWriter?.Log(this, info, true);

        if (_debugModeEnabled)
        {
            _lastInstructions.Enqueue(info);
            while (_lastInstructions.Count > 100)
            {
                _lastInstructions.Dequeue();
            }
        }
    }

    public override ushort PollForInterrupt()
    {
        if (_nmiTriggered)
        {
            _currentFrameWrittenMemory.Clear();
            _debugWriter?.Log(this, "NMI Triggered", true);
            _nmiTriggered = false;
            return 0xFFFA;
        }

        // No interrupt
        return 0;
    }
}