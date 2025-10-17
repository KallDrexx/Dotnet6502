using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Tests.Common;

public class TestHal : Base6502Hal
{
    public List<string> RaisedHooks { get; } = [];
    public ushort NextInterruptLocation { get; set; }

    public TestHal(MemoryBus memoryBus) : base(memoryBus)
    {
    }

    public override void DebugHook(string info)
    {
        RaisedHooks.Add(info);
        base.DebugHook(info);
    }

    public override ushort PollForInterrupt()
    {
        var address = NextInterruptLocation;
        NextInterruptLocation = 0;

        return address;
    }
}