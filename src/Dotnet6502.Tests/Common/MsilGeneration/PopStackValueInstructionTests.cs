using Dotnet6502.Common;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class PopStackValueInstructionTests
{
    [Fact]
    public void Can_Pop_To_Accumulator()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(123);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Pop_To_XIndex()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(55);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Pop_To_YIndex()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(88);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Pop_To_Memory()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Memory(0x3000, null, false));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(199);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)199);
    }

    [Fact]
    public void Can_Pop_To_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Memory(0x5000, Ir6502.RegisterName.XIndex, false));

        var jit = new TestJitCompiler()
        {
            TestHal =
            {
                XRegister = 10
            }
        };
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(222);
        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x500A).ShouldBe((byte)222);
    }

    [Fact]
    public void Can_Pop_To_Variable()
    {
        var popInstruction = new Ir6502.PopStackValue(new Ir6502.Variable(0));
        var copyInstruction = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [popInstruction, copyInstruction]);
        jit.TestHal.PushToStack(177);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)177);
    }

    [Fact]
    public void Can_Pop_To_Carry_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Carry));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(1);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Zero_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Zero));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(0);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(false);
    }

    [Fact]
    public void Can_Pop_To_InterruptDisable_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(1);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_BFlag_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.BFlag));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(1);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.BFlag).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Decimal_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Decimal));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(1);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Overflow_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(1);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Negative_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Negative));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(1);
        jit.RunMethod(0x1234);

        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_AllFlags()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.AllFlags());

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(0x85);
        jit.RunMethod(0x1234);

        jit.TestHal.ProcessorStatus.ShouldBe((byte)0x85);
    }

    [Fact]
    public void Can_Pop_To_StackPointer()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.StackPointer());

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);
        jit.TestHal.PushToStack(0xE5);
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0xE5);
    }
}