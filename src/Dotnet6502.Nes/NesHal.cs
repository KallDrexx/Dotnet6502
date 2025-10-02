using Dotnet6502.Common;

namespace Dotnet6502.Nes;

public class NesHal : Base6502Hal
{
    private readonly Ppu _ppu;
    private readonly CancellationToken _cancellationToken;

    public Action? NmiHandler { get; set; }

    public NesHal(NesMemory memory, Ppu ppu, CancellationToken cancellationToken)
    : base(memory)
    {
        _ppu = ppu;
        _cancellationToken = cancellationToken;
    }

    public void IncrementCpuCycleCount(int count)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("Cancellation requested");
        }

        var nmiTriggered = _ppu.RunNextStep(count);
        if (nmiTriggered)
        {
            if (NmiHandler != null)
            {
                NmiHandler();
            }
            else
            {
                throw new InvalidOperationException("NMI triggered but no NMI handler defined");
            }
        }
    }

    public void DebugHook(string info)
    {
    }
}