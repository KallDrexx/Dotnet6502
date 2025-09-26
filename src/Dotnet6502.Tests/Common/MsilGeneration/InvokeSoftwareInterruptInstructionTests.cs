using Dotnet6502.Common;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class InvokeSoftwareInterruptInstructionTests
{
    [Fact]
    public void Can_Trigger_Software_Interrupt_In_Hal()
    {
        var trigger = new Ir6502.InvokeSoftwareInterrupt();
        var testRunner = new InstructionTestRunner([trigger]);
        testRunner.RunTestMethod();

        testRunner.TestHal.SoftwareInterruptTriggered.ShouldBeTrue();
    }
}