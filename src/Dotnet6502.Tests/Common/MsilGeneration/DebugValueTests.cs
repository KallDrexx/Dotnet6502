using Dotnet6502.Common;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class DebugValueTests
{
    [Fact]
    public void Can_Call_Debug_For_Constant()
    {
        var instruction = new Ir6502.DebugValue(new Ir6502.Constant(5));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);
    }
}