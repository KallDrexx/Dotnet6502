using Dotnet6502.Common;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class InvokeSoftwareInterruptInstructionTests
{
    [Fact]
    public void Can_Trigger_Software_Interrupt_In_Hal()
    {
        var trigger = new Ir6502.InvokeSoftwareInterrupt();
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [trigger]);
        jit.RunMethod(0x1234);

        jit.TestHal.SoftwareInterruptTriggered.ShouldBeTrue();
    }
}