using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class C64Hal : Base6502Hal
{
    private readonly CancellationToken _cancellationToken;
    private readonly Vic2 _vic2;
    private readonly DebugWriter? _debugWriter;
    private readonly Queue<string> _lastInstructions = new();
    private readonly bool _debugModeEnabled;

    public C64Hal(
        MemoryBus memoryBus,
        CancellationToken cancellationToken,
        Vic2 vic2,
        DebugWriter? debugWriter,
        bool debugModeEnabled) : base(memoryBus)
    {
        _cancellationToken = cancellationToken;
        _vic2 = vic2;
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
            _vic2.RunSingleCycle();
        }
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