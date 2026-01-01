using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class UnaryInstructionTests
{
    [Fact]
    public void Can_BitwiseNot_Constant_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x55),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xAA);
    }

    [Fact]
    public void Can_BitwiseNot_Memory_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Memory(0x2000, null, false),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x2000, 0x33);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xCC);
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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [setupVar, instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xF0);
    }

    [Fact]
    public void Can_BitwiseNot_Register_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ARegister = 0xA5;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x5A);
    }

    [Fact]
    public void Can_BitwiseNot_Flag_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFE);
    }

    [Fact]
    public void Can_BitwiseNot_StackPointer_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.StackPointer(),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.StackPointer = 0xF8;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x07);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_Memory()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x77),
            new Ir6502.Memory(0x3000, null, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)0x88);
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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction, readVar]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x4000).ShouldBe((byte)0x77);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_Flag()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Zero));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true);
    }

    [Fact]
    public void Can_BitwiseNot_Constant_To_StackPointer()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x07),
            new Ir6502.StackPointer());

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0xF8);
    }

    [Fact]
    public void Can_BitwiseNot_To_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x5A),
            new Ir6502.Memory(0x6000, Ir6502.RegisterName.YIndex, false));

        var jit = TestJitCompiler.Create();
        jit.TestHal.YRegister = 5;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x6005).ShouldBe((byte)0xA5);
    }

    [Fact]
    public void Can_BitwiseNot_Zero_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0x00),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void Can_BitwiseNot_AllBitsSet_To_Register()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            new Ir6502.Constant(0xFF),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
    }

    [Fact]
    public void Can_LogicalNot_On_Zero_Value()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.LogicalNot,
            new Ir6502.Constant(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x01);
    }

    [Fact]
    public void Can_LogicalNot_On_One_Value()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.LogicalNot,
            new Ir6502.Constant(1),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
    }

    [Fact]
    public void Can_LogicalNot_On_Value_That_is_Not_One_Or_Zero()
    {
        var instruction = new Ir6502.Unary(
            Ir6502.UnaryOperator.LogicalNot,
            new Ir6502.Constant(15),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
    }
}