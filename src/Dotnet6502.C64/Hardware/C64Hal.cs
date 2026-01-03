using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class C64Hal : Base6502Hal
{
    private readonly CancellationToken _cancellationToken;
    private readonly Vic2 _vic2;

    public C64Hal(
        MemoryBus memoryBus,
        CancellationToken cancellationToken,
        Vic2 vic2) : base(memoryBus)
    {
        _cancellationToken = cancellationToken;
        _vic2 = vic2;
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
}