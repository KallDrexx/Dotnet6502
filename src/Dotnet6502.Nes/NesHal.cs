using Dotnet6502.Common;

namespace Dotnet6502.Nes;

public class NesHal : Base6502Hal
{
    private readonly Ppu _ppu;
    private readonly CancellationToken _cancellationToken;
    private readonly DebugWriter? _debugWriter;
    private bool _isInNmi;

    public Action? NmiHandler { get; set; }

    public NesHal(NesMemory memory, Ppu ppu, DebugWriter? debugWriter, CancellationToken cancellationToken)
        : base(memory)
    {
        _ppu = ppu;
        _cancellationToken = cancellationToken;
        _debugWriter = debugWriter;
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
                PushToStack(ProcessorStatus);
                _isInNmi = true;
                NmiHandler();
                _isInNmi = true;
            }
            else
            {
                throw new InvalidOperationException("NMI triggered but no NMI handler defined");
            }
        }
    }

    public void DebugHook(string info)
    {
        _debugWriter?.Log(_isInNmi, this, info, true);
    }
}