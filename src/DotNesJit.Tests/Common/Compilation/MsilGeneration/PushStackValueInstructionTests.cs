using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.MsilGeneration;

public class PushStackValueInstructionTests
{
    [Fact]
    public void Can_Push_Constant_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Constant(42));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Push_Accumulator_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                ARegister = 123
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Push_XIndex_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                XRegister = 55
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Push_YIndex_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                YRegister = 88
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Push_Memory_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Memory(0x2000, null, false));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.WriteMemory(0x2000, 156);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)156);
    }

    [Fact]
    public void Can_Push_Memory_With_Register_Offset_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Memory(0x4000, NesIr.RegisterName.XIndex, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                XRegister = 5
            }
        };
        testRunner.NesHal.WriteMemory(0x4005, 211);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)211);
    }

    [Fact]
    public void Can_Push_Variable_To_Stack()
    {
        var setupInstruction = new NesIr.Copy(
            new NesIr.Constant(144),
            new NesIr.Variable(0));
        var pushInstruction = new NesIr.PushStackValue(new NesIr.Variable(0));

        var testRunner = new InstructionTestRunner([setupInstruction, pushInstruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)144);
    }

    [Fact]
    public void Can_Push_Carry_Flag_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Flag(NesIr.FlagName.Carry));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Carry, true);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Zero_Flag_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Flag(NesIr.FlagName.Zero));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Zero, false);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)0);
    }

    [Fact]
    public void Can_Push_InterruptDisable_Flag_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Flag(NesIr.FlagName.InterruptDisable));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Overflow_Flag_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Flag(NesIr.FlagName.Overflow));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Overflow, true);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_Negative_Flag_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.Flag(NesIr.FlagName.Negative));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Negative, true);
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)1);
    }

    [Fact]
    public void Can_Push_AllFlags_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.AllFlags());

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                ProcessorStatus = 0xC3
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)0xC3);
    }

    [Fact]
    public void Can_Push_StackPointer_To_Stack()
    {
        var instruction = new NesIr.PushStackValue(new NesIr.StackPointer());

        var testRunner = new InstructionTestRunner([instruction])
        {
            NesHal =
            {
                StackPointer = 0xF8
            }
        };
        testRunner.RunTestMethod();

        testRunner.NesHal.PopFromStack().ShouldBe((byte)0xF8);
    }
}