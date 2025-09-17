using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.MsilGeneration;

public class InvokeSoftwareInterruptInstructionTests
{
    [Fact]
    public void Can_Trigger_Software_Interrupt_In_Hal()
    {
        var trigger = new NesIr.InvokeSoftwareInterrupt();
        var testRunner = new InstructionTestRunner([trigger]);
        testRunner.RunTestMethod();

        testRunner.NesHal.SoftwareInterruptTriggered.ShouldBeTrue();
    }
}