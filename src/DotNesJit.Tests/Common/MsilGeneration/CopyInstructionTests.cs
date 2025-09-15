using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.MsilGeneration;

public class CopyInstructionTests
{
    [Fact]
    public void Can_Copy_Constant_To_Accumulator()
    {
        var instruction = new NesIr.Copy(
            new NesIr.Constant(23),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)23);
    }
}