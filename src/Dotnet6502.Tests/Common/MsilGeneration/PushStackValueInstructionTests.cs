using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class PushStackValueInstructionTests
{
    [Fact]
    public void Can_Push_Constant_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Constant(42));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Push_Accumulator_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = new TestJitCompiler()
        {
            TestHal =
            {
                ARegister = 123
            }
        };
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Push_XIndex_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = new TestJitCompiler()
        {
            TestHal =
            {
                XRegister = 55
            }
        };
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Push_YIndex_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = new TestJitCompiler()
        {
            TestHal =
            {
                YRegister = 88
            }
        };
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Push_Memory_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Memory(0x2000, null, false));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x2000, 156);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Push_Memory_With_Register_Offset_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Memory(0x4000, Ir6502.RegisterName.XIndex, false));

        var jit = new TestJitCompiler()
        {
            TestHal =
            {
                XRegister = 5
            }
        };
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.WriteMemory(0x4005, 211);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)211);
    }

    [Fact]
    public void Can_Push_Variable_To_Stack()
    {
        var setupInstruction = new Ir6502.Copy(
            new Ir6502.Constant(144),
            new Ir6502.Variable(0));
        var pushInstruction = new Ir6502.PushStackValue(new Ir6502.Variable(0));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [setupInstruction, pushInstruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)144);
    }

    [Fact]
    public void Can_Push_Carry_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Carry));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Zero_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Zero));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)0);
    }

    [Fact]
    public void Can_Push_InterruptDisable_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Overflow_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Negative_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Negative));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_AllFlags_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.AllFlags());

        var jit = new TestJitCompiler()
        {
            TestHal =
            {
                ProcessorStatus = 0x85
            }
        };
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)0x85);
    }

    [Fact]
    public void Can_Push_StackPointer_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.StackPointer());

        var jit = new TestJitCompiler()
        {
            TestHal =
            {
                StackPointer = 0xF8
            }
        };
        jit.AddMethod(0x1234, [instruction]);
        jit.RunMethod(0x1234);

        jit.TestHal.PopFromStack().ShouldBe((byte)0xF8);
    }
}