using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.MsilGeneration;

public class PushStackValueInstructionTests
{
    [Fact]
    public void Can_Push_Constant_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Constant(42));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Push_Accumulator_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ARegister = 123
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Push_XIndex_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 55
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Push_YIndex_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                YRegister = 88
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Push_Memory_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Memory(0x2000, null, false));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.WriteMemory(0x2000, 156);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Push_Memory_With_Register_Offset_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Memory(0x4000, Ir6502.RegisterName.XIndex, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 5
            }
        };
        testRunner.TestHal.WriteMemory(0x4005, 211);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)211);
    }

    [Fact]
    public void Can_Push_Variable_To_Stack()
    {
        var setupInstruction = new Ir6502.Copy(
            new Ir6502.Constant(144),
            new Ir6502.Variable(0));
        var pushInstruction = new Ir6502.PushStackValue(new Ir6502.Variable(0));

        var testRunner = new InstructionTestRunner([setupInstruction, pushInstruction]);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)144);
    }

    [Fact]
    public void Can_Push_Carry_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Carry));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Zero_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Zero));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)0);
    }

    [Fact]
    public void Can_Push_InterruptDisable_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Overflow_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Negative_Flag_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.Flag(Ir6502.FlagName.Negative));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_AllFlags_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.AllFlags());

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                ProcessorStatus = 0xC3
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)0xC3);
    }

    [Fact]
    public void Can_Push_StackPointer_To_Stack()
    {
        var instruction = new Ir6502.PushStackValue(new Ir6502.StackPointer());

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                StackPointer = 0xF8
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.PopFromStack().ShouldBe((byte)0xF8);
    }
}