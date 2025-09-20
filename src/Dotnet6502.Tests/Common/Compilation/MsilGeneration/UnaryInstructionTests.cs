using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.MsilGeneration;

public class UnaryInstructionTests
{
    [Fact]
    public void Can_BitwiseNot_Constant_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x55),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xAA);
    }

    [Fact]
    public void Can_BitwiseNot_Memory_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Memory(0x2000, null, false),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.WriteMemory(0x2000, 0x33);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0xCC);
    }

    [Fact]
    public void Can_BitwiseNot_Variable_To_Register()
    {
        var setupVar = new Ir6502.Copy(
            new Ir6502.Constant(0x0F),
            new Ir6502.Variable(0));
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setupVar, instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0xF0);
    }

    [Fact]
    public void Can_BitwiseNot_Register_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                ARegister = 0xA5
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x5A);
    }

    [Fact]
    public void Can_BitwiseNot_Flag_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Carry, true);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFE);
    }

    [Fact]
    public void Can_BitwiseNot_AllFlags_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.AllFlags(),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                ProcessorStatus = 0xC3
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x3C);
    }

    [Fact]
    public void Can_BitwiseNot_StackPointer_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.StackPointer(),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                StackPointer = 0xF8
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x07);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_Memory()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x77),
            new Ir6502.Memory(0x3000, null, false));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x3000).ShouldBe((byte)0x88);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_Variable()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x88),
            new Ir6502.Variable(0));
        var readVar = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Memory(0x4000, null, false));

        var testRunner = new InstructionTestRunner([instruction, readVar]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x4000).ShouldBe((byte)0x77);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_Flag()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Zero));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_AllFlags()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x81),
            new Ir6502.AllFlags());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ProcessorStatus.ShouldBe((byte)0x7E);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_StackPointer()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x07),
            new Ir6502.StackPointer());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0xF8);
    }

    [Fact]
    public void Can_BitwiseNot_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Memory(0x5000, Ir6502.RegisterName.XIndex, false),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                XRegister = 10
            }
        };
        testRunner.NesHal.WriteMemory(0x500A, 0x3C);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xC3);
    }

    [Fact]
    public void Can_BitwiseNot_To_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x5A),
            new Ir6502.Memory(0x6000, Ir6502.RegisterName.YIndex, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                YRegister = 5
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x6005).ShouldBe((byte)0xA5);
    }

    [Fact]
    public void Can_BitwiseNot_Zero_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x00),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void Can_BitwiseNot_AllBitsSet_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0xFF),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
    }
}