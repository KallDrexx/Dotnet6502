using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.MsilGeneration;

public class ReturnInstructionTests
{
    [Fact]
    public void Return_Stops_Before_Subsequent_Instructions()
    {
        var setAccumulator = new Ir6502.Copy(
            new Ir6502.Constant(77),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var setMemory = new Ir6502.Copy(
            new Ir6502.Constant(88),
            new Ir6502.Memory(0x4000, null, false));

        var setFlag = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Flag(Ir6502.FlagName.Negative));

        var setVariable = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Variable(0));

        var returnInstruction = new Ir6502.Return();
        var modifyAccumulator = new Ir6502.Copy(
            new Ir6502.Constant(11),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var modifyMemory = new Ir6502.Copy(
            new Ir6502.Constant(22),
            new Ir6502.Memory(0x4000, null, false));

        var clearFlag = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Negative));

        var binaryOperation = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Variable(0),
            new Ir6502.Constant(50),
            new Ir6502.Variable(1));

        var copyVariableToRegister = new Ir6502.Copy(
            new Ir6502.Variable(1),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

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