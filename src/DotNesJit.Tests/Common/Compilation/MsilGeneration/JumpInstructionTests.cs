using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.MsilGeneration;

public class JumpInstructionTests
{
    [Fact]
    public void Can_Jump_Forward_To_Label()
    {
        var jump = new NesIr.Jump(new NesIr.Identifier("forward"));
        var copy1 = new NesIr.Copy(
            new NesIr.Constant(11),
            new NesIr.Register(NesIr.RegisterName.YIndex));
        var label = new NesIr.Label(new NesIr.Identifier("forward"));
        var copy2 = new NesIr.Copy(
            new NesIr.Constant(22),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([jump, copy1, label, copy2]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0);
        testRunner.NesHal.ARegister.ShouldBe((byte)22);
    }

    [Fact]
    public void Can_Jump_Backward_To_Label()
    {
        var copy1 = new NesIr.Copy(
            new NesIr.Constant(1),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var label = new NesIr.Label(new NesIr.Identifier("loop"));
        var copy2 = new NesIr.Copy(
            new NesIr.Register(NesIr.RegisterName.XIndex),
            new NesIr.Variable(0));
        var addVar = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(1),
            new NesIr.Variable(0));
        var copyBack = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var comparison = new NesIr.Binary(
            NesIr.BinaryOperator.LessThan,
            new NesIr.Register(NesIr.RegisterName.XIndex),
            new NesIr.Constant(3),
            new NesIr.Variable(1));
        var conditionalJump = new NesIr.JumpIfNotZero(
            new NesIr.Variable(1),
            new NesIr.Identifier("loop"));

        var testRunner = new InstructionTestRunner([copy1, label, copy2, addVar, copyBack, comparison, conditionalJump]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)3);
    }

    [Fact]
    public void Can_Handle_Multiple_Labels()
    {
        var label1 = new NesIr.Label(new NesIr.Identifier("first"));
        var copy1 = new NesIr.Copy(
            new NesIr.Constant(10),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var jump = new NesIr.Jump(new NesIr.Identifier("second"));
        var label2 = new NesIr.Label(new NesIr.Identifier("second"));
        var copy2 = new NesIr.Copy(
            new NesIr.Constant(20),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([label1, copy1, jump, label2, copy2]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)10);
        testRunner.NesHal.XRegister.ShouldBe((byte)20);
    }

    [Fact]
    public void JumpIfZero_Jumps_When_Condition_Is_Zero()
    {
        var setCondition = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var jumpIfZero = new NesIr.JumpIfZero(
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            new NesIr.Identifier("target"));
        var skipInstruction = new NesIr.Copy(
            new NesIr.Constant(99),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var label = new NesIr.Label(new NesIr.Identifier("target"));
        var executeInstruction = new NesIr.Copy(
            new NesIr.Constant(42),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setCondition, jumpIfZero, skipInstruction, label, executeInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0);
        testRunner.NesHal.XRegister.ShouldBe((byte)0);
        testRunner.NesHal.YRegister.ShouldBe((byte)42);
    }

    [Fact]
    public void JumpIfZero_Does_Not_Jump_When_Condition_Is_Not_Zero()
    {
        var setCondition = new NesIr.Copy(
            new NesIr.Constant(1),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var jumpIfZero = new NesIr.JumpIfZero(
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            new NesIr.Identifier("target"));
        var executeInstruction = new NesIr.Copy(
            new NesIr.Constant(77),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var label = new NesIr.Label(new NesIr.Identifier("target"));
        var skipInstruction = new NesIr.Copy(
            new NesIr.Constant(88),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setCondition, jumpIfZero, executeInstruction, label, skipInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)1);
        testRunner.NesHal.XRegister.ShouldBe((byte)77);
        testRunner.NesHal.YRegister.ShouldBe((byte)88);
    }

    [Fact]
    public void JumpIfNotZero_Jumps_When_Condition_Is_Not_Zero()
    {
        var setCondition = new NesIr.Copy(
            new NesIr.Constant(5),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var jumpIfNotZero = new NesIr.JumpIfNotZero(
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            new NesIr.Identifier("target"));
        var skipInstruction = new NesIr.Copy(
            new NesIr.Constant(11),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var label = new NesIr.Label(new NesIr.Identifier("target"));
        var executeInstruction = new NesIr.Copy(
            new NesIr.Constant(33),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setCondition, jumpIfNotZero, skipInstruction, label, executeInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)5);
        testRunner.NesHal.XRegister.ShouldBe((byte)0);
        testRunner.NesHal.YRegister.ShouldBe((byte)33);
    }

    [Fact]
    public void JumpIfNotZero_Does_Not_Jump_When_Condition_Is_Zero()
    {
        var setCondition = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var jumpIfNotZero = new NesIr.JumpIfNotZero(
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            new NesIr.Identifier("target"));
        var executeInstruction = new NesIr.Copy(
            new NesIr.Constant(44),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var label = new NesIr.Label(new NesIr.Identifier("target"));
        var skipInstruction = new NesIr.Copy(
            new NesIr.Constant(55),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setCondition, jumpIfNotZero, executeInstruction, label, skipInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0);
        testRunner.NesHal.XRegister.ShouldBe((byte)44);
        testRunner.NesHal.YRegister.ShouldBe((byte)55);
    }

    [Fact]
    public void JumpIfZero_With_Memory_Condition()
    {
        var setMemory = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Memory(0x2000, null));
        var jumpIfZero = new NesIr.JumpIfZero(
            new NesIr.Memory(0x2000, null),
            new NesIr.Identifier("jump_target"));
        var skipInstruction = new NesIr.Copy(
            new NesIr.Constant(100),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var label = new NesIr.Label(new NesIr.Identifier("jump_target"));
        var executeInstruction = new NesIr.Copy(
            new NesIr.Constant(200),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([setMemory, jumpIfZero, skipInstruction, label, executeInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x2000).ShouldBe((byte)0);
        testRunner.NesHal.ARegister.ShouldBe((byte)0);
        testRunner.NesHal.XRegister.ShouldBe((byte)200);
    }

    [Fact]
    public void JumpIfNotZero_With_Variable_Condition()
    {
        var setVariable = new NesIr.Copy(
            new NesIr.Constant(7),
            new NesIr.Variable(0));
        var jumpIfNotZero = new NesIr.JumpIfNotZero(
            new NesIr.Variable(0),
            new NesIr.Identifier("jump_target"));
        var skipInstruction = new NesIr.Copy(
            new NesIr.Constant(111),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var label = new NesIr.Label(new NesIr.Identifier("jump_target"));
        var executeInstruction = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setVariable, jumpIfNotZero, skipInstruction, label, executeInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0);
        testRunner.NesHal.YRegister.ShouldBe((byte)7);
    }

    [Fact]
    public void JumpIfZero_With_Flag_Condition()
    {
        var setFlag = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Flag(NesIr.FlagName.Carry));
        var jumpIfZero = new NesIr.JumpIfZero(
            new NesIr.Flag(NesIr.FlagName.Carry),
            new NesIr.Identifier("flag_target"));
        var skipInstruction = new NesIr.Copy(
            new NesIr.Constant(123),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var label = new NesIr.Label(new NesIr.Identifier("flag_target"));
        var executeInstruction = new NesIr.Copy(
            new NesIr.Constant(156),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([setFlag, jumpIfZero, skipInstruction, label, executeInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(false);
        testRunner.NesHal.ARegister.ShouldBe((byte)0);
        testRunner.NesHal.XRegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Conditional_Jump_Loop_With_Counter()
    {
        var initCounter = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var label = new NesIr.Label(new NesIr.Identifier("loop"));
        var incrementCounter = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Register(NesIr.RegisterName.XIndex),
            new NesIr.Constant(1),
            new NesIr.Register(NesIr.RegisterName.XIndex));
        var checkCondition = new NesIr.Binary(
            NesIr.BinaryOperator.LessThan,
            new NesIr.Register(NesIr.RegisterName.XIndex),
            new NesIr.Constant(3),
            new NesIr.Variable(0));
        var conditionalJump = new NesIr.JumpIfNotZero(
            new NesIr.Variable(0),
            new NesIr.Identifier("loop"));
        var finalCopy = new NesIr.Copy(
            new NesIr.Register(NesIr.RegisterName.XIndex),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([initCounter, label, incrementCounter, checkCondition, conditionalJump, finalCopy]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)3);
        testRunner.NesHal.ARegister.ShouldBe((byte)3);
    }
}