using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.MsilGeneration;

public class BinaryInstructionTests
{
    [Fact]
    public void Can_Add_Constants_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(15),
            new NesIr.Constant(25),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)40);
    }

    [Fact]
    public void Can_Subtract_Constants_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            new NesIr.Constant(50),
            new NesIr.Constant(30),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)20);
    }

    [Fact]
    public void Can_And_Constants_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            new NesIr.Constant(0xFF),
            new NesIr.Constant(0x0F),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x0F);
    }

    [Fact]
    public void Can_Or_Constants_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Or,
            new NesIr.Constant(0x0F),
            new NesIr.Constant(0xF0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void Can_Xor_Constants_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Xor,
            new NesIr.Constant(0xAA),
            new NesIr.Constant(0x55),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void Can_ShiftLeft_Constants_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftLeft,
            new NesIr.Constant(0x01),
            new NesIr.Constant(4),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x10);
    }

    [Fact]
    public void Can_ShiftRight_Constants_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftRight,
            new NesIr.Constant(0x80),
            new NesIr.Constant(4),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x08);
    }

    [Fact]
    public void Can_Equals_Constants_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            new NesIr.Constant(42),
            new NesIr.Constant(42),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Equals_Constants_False_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            new NesIr.Constant(42),
            new NesIr.Constant(43),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_NotEquals_Constants_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.NotEquals,
            new NesIr.Constant(42),
            new NesIr.Constant(43),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_NotEquals_Constants_False_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.NotEquals,
            new NesIr.Constant(42),
            new NesIr.Constant(42),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_GreaterThan_Constants_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThan,
            new NesIr.Constant(50),
            new NesIr.Constant(30),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_GreaterThan_Constants_False_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThan,
            new NesIr.Constant(30),
            new NesIr.Constant(50),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_GreaterThanOrEqualTo_Constants_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThanOrEqualTo,
            new NesIr.Constant(50),
            new NesIr.Constant(50),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_GreaterThanOrEqualTo_Constants_False_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThanOrEqualTo,
            new NesIr.Constant(30),
            new NesIr.Constant(50),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_GreaterThanOrEqualTo_Constants_GreaterThan_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThanOrEqualTo,
            new NesIr.Constant(60),
            new NesIr.Constant(50),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_LessThan_Constants_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.LessThan,
            new NesIr.Constant(30),
            new NesIr.Constant(50),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_LessThan_Constants_False_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.LessThan,
            new NesIr.Constant(50),
            new NesIr.Constant(30),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_LessThanOrEqualTo_Constants_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.LessThanOrEqualTo,
            new NesIr.Constant(30),
            new NesIr.Constant(30),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_LessThanOrEqualTo_Constants_False_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.LessThanOrEqualTo,
            new NesIr.Constant(50),
            new NesIr.Constant(30),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_LessThanOrEqualTo_Constants_LessThan_True_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.LessThanOrEqualTo,
            new NesIr.Constant(20),
            new NesIr.Constant(30),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Add_Register_And_Constant_To_Memory()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            new NesIr.Constant(10),
            new NesIr.Memory(0x1000, null));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.ARegister = 25;
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x1000).ShouldBe((byte)35);
    }

    [Fact]
    public void Can_Add_Constants_To_Variable()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(12),
            new NesIr.Constant(8),
            new NesIr.Variable(0));

        var readVar = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)20);
    }

    [Fact]
    public void Can_Add_Memory_Values_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Memory(0x2000, null),
            new NesIr.Memory(0x2001, null),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.WriteMemory(0x2000, 15);
        testRunner.NesHal.WriteMemory(0x2001, 20);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)35);
    }

    [Fact]
    public void Can_Add_Memory_With_Register_Offset_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Memory(0x3000, NesIr.RegisterName.XIndex),
            new NesIr.Constant(5),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.XRegister = 10;
        testRunner.NesHal.WriteMemory(0x300A, 30);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)35);
    }

    [Fact]
    public void Can_Add_Variables_To_Register()
    {
        var setupVar1 = new NesIr.Copy(
            new NesIr.Constant(15),
            new NesIr.Variable(0));
        var setupVar2 = new NesIr.Copy(
            new NesIr.Constant(25),
            new NesIr.Variable(1));
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Variable(1),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupVar1, setupVar2, instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)40);
    }

    [Fact]
    public void Can_Add_Flag_Values_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Flag(NesIr.FlagName.Carry),
            new NesIr.Flag(NesIr.FlagName.Zero),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Carry, true);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Zero, true);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)2);
    }

    [Fact]
    public void Can_Add_AllFlags_And_Constant_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.AllFlags(),
            new NesIr.Constant(10),
            new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.ProcessorStatus = 0x20;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Add_StackPointer_And_Constant_To_Register()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.StackPointer(),
            new NesIr.Constant(5),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.StackPointer = 0xF0;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xF5);
    }

    [Fact]
    public void Can_Binary_Operation_To_Flag()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            new NesIr.Constant(42),
            new NesIr.Constant(42),
            new NesIr.Flag(NesIr.FlagName.Zero));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true);
    }

    [Fact]
    public void Can_Binary_Operation_To_AllFlags()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(0x80),
            new NesIr.Constant(0x03),
            new NesIr.AllFlags());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ProcessorStatus.ShouldBe((byte)0x83);
    }

    [Fact]
    public void Can_Binary_Operation_To_StackPointer()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(0xF0),
            new NesIr.Constant(0x05),
            new NesIr.StackPointer());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0xF5);
    }

    [Fact]
    public void Can_Binary_Operation_To_Memory_With_Register_Offset()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(10),
            new NesIr.Constant(20),
            new NesIr.Memory(0x4000, NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.YRegister = 15;
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x400F).ShouldBe((byte)30);
    }

    [Fact]
    public void Can_Handle_Overflow_In_Addition()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(200),
            new NesIr.Constant(100),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)44);
    }

    [Fact]
    public void Can_Handle_Underflow_In_Subtraction()
    {
        var instruction = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            new NesIr.Constant(10),
            new NesIr.Constant(20),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)246);
    }
}