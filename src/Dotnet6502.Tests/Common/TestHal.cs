using Dotnet6502.Common;

namespace Dotnet6502.Tests.Common;

public class TestHal : Base6502Hal
{
    public List<string> RaisedHooks { get; } = [];

    public TestHal(IMemoryMap memoryMap) : base(memoryMap)
    {
    }

    public override void DebugHook(string info)
    {
        RaisedHooks.Add(info);
        base.DebugHook(info);
    }
}