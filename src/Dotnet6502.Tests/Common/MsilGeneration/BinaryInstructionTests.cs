using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class BinaryInstructionTests
{
    [Fact]
    public void Can_Add_Constants_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Constant(15),
            new Ir6502.Constant(25),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)40);
    }

    [Fact]
    public void Can_Subtract_Constants_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            new Ir6502.Constant(50),
            new Ir6502.Constant(30),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)20);
    }

    [Fact]
    public void Can_And_Constants_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            new Ir6502.Constant(0xFF),
            new Ir6502.Constant(0x0F),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x0F);
    }

    [Fact]
    public void Can_Or_Constants_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Or,
            new Ir6502.Constant(0x0F),
            new Ir6502.Constant(0xF0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void Can_Xor_Constants_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Xor,
            new Ir6502.Constant(0xAA),
            new Ir6502.Constant(0x55),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void Can_ShiftLeft_Constants_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.ShiftLeft,
            new Ir6502.Constant(0x01),
            new Ir6502.Constant(4),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x10);
    }

    [Fact]
    public void Can_ShiftRight_Constants_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.ShiftRight,
            new Ir6502.Constant(0x80),
            new Ir6502.Constant(4),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x08);
    }

    [Fact]
    public void Can_Equals_Constants_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            new Ir6502.Constant(42),
            new Ir6502.Constant(42),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Equals_Constants_False_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            new Ir6502.Constant(42),
            new Ir6502.Constant(43),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_NotEquals_Constants_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.NotEquals,
            new Ir6502.Constant(42),
            new Ir6502.Constant(43),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_NotEquals_Constants_False_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.NotEquals,
            new Ir6502.Constant(42),
            new Ir6502.Constant(42),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_GreaterThan_Constants_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThan,
            new Ir6502.Constant(50),
            new Ir6502.Constant(30),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_GreaterThan_Constants_False_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThan,
            new Ir6502.Constant(30),
            new Ir6502.Constant(50),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_GreaterThanOrEqualTo_Constants_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThanOrEqualTo,
            new Ir6502.Constant(50),
            new Ir6502.Constant(50),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_GreaterThanOrEqualTo_Constants_False_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThanOrEqualTo,
            new Ir6502.Constant(30),
            new Ir6502.Constant(50),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_GreaterThanOrEqualTo_Constants_GreaterThan_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThanOrEqualTo,
            new Ir6502.Constant(60),
            new Ir6502.Constant(50),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_LessThan_Constants_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.LessThan,
            new Ir6502.Constant(30),
            new Ir6502.Constant(50),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_LessThan_Constants_False_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.LessThan,
            new Ir6502.Constant(50),
            new Ir6502.Constant(30),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_LessThanOrEqualTo_Constants_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.LessThanOrEqualTo,
            new Ir6502.Constant(30),
            new Ir6502.Constant(30),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_LessThanOrEqualTo_Constants_False_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.LessThanOrEqualTo,
            new Ir6502.Constant(50),
            new Ir6502.Constant(30),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_LessThanOrEqualTo_Constants_LessThan_True_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.LessThanOrEqualTo,
            new Ir6502.Constant(20),
            new Ir6502.Constant(30),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Add_Register_And_Constant_To_Memory()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            new Ir6502.Constant(10),
            new Ir6502.Memory(0x1000, null, false));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ARegister = 25;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x1000).ShouldBe((byte)35);
    }

    [Fact]
    public void Can_Add_Constants_To_Variable()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Constant(12),
            new Ir6502.Constant(8),
            new Ir6502.Variable(0));

        var readVar = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction, readVar]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)20);
    }

    [Fact]
    public void Can_Add_Memory_Values_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Memory(0x2000, null, false),
            new Ir6502.Memory(0x2001, null, false),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x2000, 15);
        jit.TestHal.WriteMemory(0x2001, 20);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)35);
    }

    [Fact]
    public void Can_Add_Memory_With_Register_Offset_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Memory(0x3000, Ir6502.RegisterName.XIndex, false),
            new Ir6502.Constant(5),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.XRegister = 10;
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x300A, 30);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)35);
    }

    [Fact]
    public void Can_Add_Variables_To_Register()
    {
        var setupVar1 = new Ir6502.Copy(
            new Ir6502.Constant(15),
            new Ir6502.Variable(0));
        var setupVar2 = new Ir6502.Copy(
            new Ir6502.Constant(25),
            new Ir6502.Variable(1));
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Variable(0),
            new Ir6502.Variable(1),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [setupVar1, setupVar2, instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)40);
    }

    [Fact]
    public void Can_Add_Flag_Values_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Flag(Ir6502.FlagName.Zero),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)2);
    }

    [Fact]
    public void Can_Add_AllFlags_And_Constant_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.AllFlags(),
            new Ir6502.Constant(0x85),
            new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.ProcessorStatus = 0x20;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xA5);
    }

    [Fact]
    public void Can_Add_StackPointer_And_Constant_To_Register()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.StackPointer(),
            new Ir6502.Constant(5),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.StackPointer = 0xF0;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xF5);
    }

    [Fact]
    public void Can_Binary_Operation_To_Flag()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            new Ir6502.Constant(42),
            new Ir6502.Constant(42),
            new Ir6502.Flag(Ir6502.FlagName.Zero));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true);
    }

    [Fact]
    public void Can_Binary_Operation_To_StackPointer()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Constant(0xF0),
            new Ir6502.Constant(0x05),
            new Ir6502.StackPointer());

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0xF5);
    }

    [Fact]
    public void Can_Binary_Operation_To_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Constant(10),
            new Ir6502.Constant(20),
            new Ir6502.Memory(0x4000, Ir6502.RegisterName.YIndex, false));

        var jit = TestJitCompiler.Create();
        jit.TestHal.YRegister = 15;
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x400F).ShouldBe((byte)30);
    }

    [Fact]
    public void Can_Handle_Overflow_In_Addition()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            new Ir6502.Constant(200),
            new Ir6502.Constant(100),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)44);
    }

    [Fact]
    public void Can_Handle_Underflow_In_Subtraction()
    {
        var instruction = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            new Ir6502.Constant(10),
            new Ir6502.Constant(20),
            new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)246);
    }
}