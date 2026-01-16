using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class C64Hal : Base6502Hal
{
    private readonly CancellationToken _cancellationToken;
    private readonly Vic2 _vic2;
    private readonly IoMemoryArea _ioMemoryArea;
    private readonly DebugWriter? _debugWriter;
    private readonly Queue<string> _lastInstructions = new();
    private readonly bool _debugModeEnabled;

    public C64Hal(
        C64MemoryConfig memoryConfig,
        CancellationToken cancellationToken,
        Vic2 vic2,
        DebugWriter? debugWriter,
        bool debugModeEnabled) : base(memoryConfig.CpuMemoryBus)
    {
        _cancellationToken = cancellationToken;
        _vic2 = vic2;
        _ioMemoryArea = memoryConfig.IoMemoryArea;
        _debugWriter = debugWriter;
        _debugModeEnabled = debugModeEnabled;
    }

    /// <summary>
    /// Increments the CPU cycle count in preparation for the next instruction, and will
    /// run any peripherals that need to stay in sync with the CPU.
    /// </summary>
    public void IncrementCpuCycleCount(int count)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("Cancellation requested");
        }

        for (var x = 0; x < count; x++)
        {
            var badLineStarted = _vic2.RunSingleCycle();
            _ioMemoryArea.Cia1.RunCycle();
            _ioMemoryArea.Cia2.RunCycle();

            if (badLineStarted)
            {
                for (var badLineCount = 0; badLineCount < 40; badLineCount++)
                {
                    _vic2.RunSingleCycle();
                    _ioMemoryArea.Cia1.RunCycle();
                    _ioMemoryArea.Cia2.RunCycle();
                }
            }
        }
    }

    public override ushort PollForInterrupt()
    {
        if (_vic2.IrqTriggered && !GetFlag(CpuStatusFlags.InterruptDisable))
        {
            _debugWriter?.Log(this, "IRQ Triggered", true);
            return 0xFFFE;
        }

        // No interrupt
        return 0;
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
}