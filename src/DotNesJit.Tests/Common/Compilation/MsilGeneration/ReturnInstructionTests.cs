using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.MsilGeneration;

public class ReturnInstructionTests
{
    [Fact]
    public void Return_Stops_Multiple_Different_Instruction_Types()
    {
        var setAccumulator = new NesIr.Copy(
            new NesIr.Constant(77),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var setMemory = new NesIr.Copy(
            new NesIr.Constant(88),
            new NesIr.Memory(0x4000, null));

        var setFlag = new NesIr.Copy(
            new NesIr.Constant(1),
            new NesIr.Flag(NesIr.FlagName.Negative));

        var setVariable = new NesIr.Copy(
            new NesIr.Constant(99),
            new NesIr.Variable(0));

        var returnInstruction = new NesIr.Return();
        var modifyAccumulator = new NesIr.Copy(
            new NesIr.Constant(11),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var modifyMemory = new NesIr.Copy(
            new NesIr.Constant(22),
            new NesIr.Memory(0x4000, null));

        var clearFlag = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Flag(NesIr.FlagName.Negative));

        var binaryOperation = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(50),
            new NesIr.Variable(1));

        var copyVariableToRegister = new NesIr.Copy(
            new NesIr.Variable(1),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([
            setAccumulator, setMemory, setFlag, setVariable,
            returnInstruction,
            modifyAccumulator, modifyMemory, clearFlag, binaryOperation, copyVariableToRegister
        ]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)77);
        testRunner.NesHal.ReadMemory(0x4000).ShouldBe((byte)88);
        testRunner.NesHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(true);
        testRunner.NesHal.YRegister.ShouldBe((byte)0);
    }
}