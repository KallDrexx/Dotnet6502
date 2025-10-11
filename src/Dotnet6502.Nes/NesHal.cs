using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Nes;

public class NesHal : Base6502Hal
{
    private readonly Ppu _ppu;
    private readonly CancellationToken _cancellationToken;
    private readonly DebugWriter? _debugWriter;
    private bool _nmiTriggered;

    public Action? NmiHandler { get; set; }

    public NesHal(NesMemory memory, Ppu ppu, DebugWriter? debugWriter, CancellationToken cancellationToken)
        : base(memory)
    {
        _ppu = ppu;
        _cancellationToken = cancellationToken;
        _debugWriter = debugWriter;
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

    public override void DebugHook(string info)
    {
        _debugWriter?.Log(this, info, true);
    }

    public override ushort PollForInterrupt()
    {
        if (_nmiTriggered)
        {
            _debugWriter?.Log(this, "NMI Triggered", true);
            _nmiTriggered = false;
            return 0xFFFA;
        }

        // No interrupt
        return 0;
    }
}