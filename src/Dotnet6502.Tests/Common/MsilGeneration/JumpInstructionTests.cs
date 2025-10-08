using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class JumpInstructionTests
{
    [Fact]
    public void Can_Jump_Forward_To_Label()
    {
        var jump = new Ir6502.Jump(new Ir6502.Identifier("forward"));
        var copy1 = new Ir6502.Copy(
            new Ir6502.Constant(11),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));
        var label = new Ir6502.Label(new Ir6502.Identifier("forward"));
        var copy2 = new Ir6502.Copy(
            new Ir6502.Constant(22),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [jump, copy1, label, copy2]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0);
        jit.TestHal.ARegister.ShouldBe((byte)22);
    }

    [Fact]
    public void Can_Jump_Backward_To_Label()
    {
        var copy1 = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var label = new Ir6502.Label(new Ir6502.Identifier("loop"));
        var copy2 = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Variable(0));
        var addVar = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Variable(0),
            new Ir6502.Constant(1),
            new Ir6502.Variable(0));
        var copyBack = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var comparison = new Ir6502.Binary(
            Ir6502.BinaryOperator.LessThan,
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Constant(3),
            new Ir6502.Variable(1));
        var conditionalJump = new Ir6502.JumpIfNotZero(
            new Ir6502.Variable(1),
            new Ir6502.Identifier("loop"));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [copy1, label, copy2, addVar, copyBack, comparison, conditionalJump]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)3);
    }

    [Fact]
    public void Can_Handle_Multiple_Labels()
    {
        var label1 = new Ir6502.Label(new Ir6502.Identifier("first"));
        var copy1 = new Ir6502.Copy(
            new Ir6502.Constant(10),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var jump = new Ir6502.Jump(new Ir6502.Identifier("second"));
        var label2 = new Ir6502.Label(new Ir6502.Identifier("second"));
        var copy2 = new Ir6502.Copy(
            new Ir6502.Constant(20),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [label1, copy1, jump, label2, copy2]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)10);
        jit.TestHal.XRegister.ShouldBe((byte)20);
    }

    [Fact]
    public void JumpIfZero_Jumps_When_Condition_Is_Zero()
    {
        var setCondition = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var jumpIfZero = new Ir6502.JumpIfZero(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Identifier("target"));
        var skipInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var label = new Ir6502.Label(new Ir6502.Identifier("target"));
        var executeInstruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setCondition, jumpIfZero, skipInstruction, label, executeInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0);
        jit.TestHal.XRegister.ShouldBe((byte)0);
        jit.TestHal.YRegister.ShouldBe((byte)42);
    }

    [Fact]
    public void JumpIfZero_Does_Not_Jump_When_Condition_Is_Not_Zero()
    {
        var setCondition = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var jumpIfZero = new Ir6502.JumpIfZero(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Identifier("target"));
        var executeInstruction = new Ir6502.Copy(
            new Ir6502.Constant(77),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var label = new Ir6502.Label(new Ir6502.Identifier("target"));
        var skipInstruction = new Ir6502.Copy(
            new Ir6502.Constant(88),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setCondition, jumpIfZero, executeInstruction, label, skipInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)1);
        jit.TestHal.XRegister.ShouldBe((byte)77);
        jit.TestHal.YRegister.ShouldBe((byte)88);
    }

    [Fact]
    public void JumpIfNotZero_Jumps_When_Condition_Is_Not_Zero()
    {
        var setCondition = new Ir6502.Copy(
            new Ir6502.Constant(5),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var jumpIfNotZero = new Ir6502.JumpIfNotZero(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Identifier("target"));
        var skipInstruction = new Ir6502.Copy(
            new Ir6502.Constant(11),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var label = new Ir6502.Label(new Ir6502.Identifier("target"));
        var executeInstruction = new Ir6502.Copy(
            new Ir6502.Constant(33),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setCondition, jumpIfNotZero, skipInstruction, label, executeInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)5);
        jit.TestHal.XRegister.ShouldBe((byte)0);
        jit.TestHal.YRegister.ShouldBe((byte)33);
    }

    [Fact]
    public void JumpIfNotZero_Does_Not_Jump_When_Condition_Is_Zero()
    {
        var setCondition = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var jumpIfNotZero = new Ir6502.JumpIfNotZero(
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Identifier("target"));
        var executeInstruction = new Ir6502.Copy(
            new Ir6502.Constant(44),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var label = new Ir6502.Label(new Ir6502.Identifier("target"));
        var skipInstruction = new Ir6502.Copy(
            new Ir6502.Constant(55),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setCondition, jumpIfNotZero, executeInstruction, label, skipInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0);
        jit.TestHal.XRegister.ShouldBe((byte)44);
        jit.TestHal.YRegister.ShouldBe((byte)55);
    }

    [Fact]
    public void JumpIfZero_With_Memory_Condition()
    {
        var setMemory = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Memory(0x2000, null, false));
        var jumpIfZero = new Ir6502.JumpIfZero(
            new Ir6502.Memory(0x2000, null, false),
            new Ir6502.Identifier("jump_target"));
        var skipInstruction = new Ir6502.Copy(
            new Ir6502.Constant(100),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var label = new Ir6502.Label(new Ir6502.Identifier("jump_target"));
        var executeInstruction = new Ir6502.Copy(
            new Ir6502.Constant(200),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setMemory, jumpIfZero, skipInstruction, label, executeInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x2000).ShouldBe((byte)0);
        jit.TestHal.ARegister.ShouldBe((byte)0);
        jit.TestHal.XRegister.ShouldBe((byte)200);
    }

    [Fact]
    public void JumpIfNotZero_With_Variable_Condition()
    {
        var setVariable = new Ir6502.Copy(
            new Ir6502.Constant(7),
            new Ir6502.Variable(0));
        var jumpIfNotZero = new Ir6502.JumpIfNotZero(
            new Ir6502.Variable(0),
            new Ir6502.Identifier("jump_target"));
        var skipInstruction = new Ir6502.Copy(
            new Ir6502.Constant(111),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var label = new Ir6502.Label(new Ir6502.Identifier("jump_target"));
        var executeInstruction = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setVariable, jumpIfNotZero, skipInstruction, label, executeInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0);
        jit.TestHal.YRegister.ShouldBe((byte)7);
    }

    [Fact]
    public void JumpIfZero_With_Flag_Condition()
    {
        var setFlag = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Carry));
        var jumpIfZero = new Ir6502.JumpIfZero(
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Identifier("flag_target"));
        var skipInstruction = new Ir6502.Copy(
            new Ir6502.Constant(123),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var label = new Ir6502.Label(new Ir6502.Identifier("flag_target"));
        var executeInstruction = new Ir6502.Copy(
            new Ir6502.Constant(156),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setFlag, jumpIfZero, skipInstruction, label, executeInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(false);
        jit.TestHal.ARegister.ShouldBe((byte)0);
        jit.TestHal.XRegister.ShouldBe((byte)156);
    }

    [Fact]
    public void Conditional_Jump_Loop_With_Counter()
    {
        var initCounter = new Ir6502.Copy(
            new Ir6502.Constant(0),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var label = new Ir6502.Label(new Ir6502.Identifier("loop"));
        var incrementCounter = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Constant(1),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));
        var checkCondition = new Ir6502.Binary(
            Ir6502.BinaryOperator.LessThan,
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Constant(3),
            new Ir6502.Variable(0));
        var conditionalJump = new Ir6502.JumpIfNotZero(
            new Ir6502.Variable(0),
            new Ir6502.Identifier("loop"));
        var finalCopy = new Ir6502.Copy(
            new Ir6502.Register(Ir6502.RegisterName.XIndex),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [initCounter, label, incrementCounter, checkCondition, conditionalJump, finalCopy]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)3);
        jit.TestHal.ARegister.ShouldBe((byte)3);
    }
}