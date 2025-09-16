using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.MsilGeneration;

public class PopStackValueInstructionTests
{
    [Fact]
    public void Can_Pop_To_Accumulator()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(123);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Pop_To_XIndex()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(55);
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Pop_To_YIndex()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Register(NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(88);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Pop_To_Memory()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Memory(0x3000, null));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(199);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x3000).ShouldBe((byte)199);
    }

    [Fact]
    public void Can_Pop_To_Memory_With_Register_Offset()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Memory(0x5000, NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.XRegister = 10;
        testRunner.NesHal.PushToStack(222);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x500A).ShouldBe((byte)222);
    }

    [Fact]
    public void Can_Pop_To_Variable()
    {
        var popInstruction = new NesIr.PopStackValue(new NesIr.Variable(0));
        var copyInstruction = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([popInstruction, copyInstruction]);
        testRunner.NesHal.PushToStack(177);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)177);
    }

    [Fact]
    public void Can_Pop_To_Carry_Flag()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Flag(NesIr.FlagName.Carry));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Zero_Flag()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Flag(NesIr.FlagName.Zero));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(0);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(false);
    }

    [Fact]
    public void Can_Pop_To_InterruptDisable_Flag()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Flag(NesIr.FlagName.InterruptDisable));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_BFlag_Flag()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Flag(NesIr.FlagName.BFlag));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.BFlag).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Decimal_Flag()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Flag(NesIr.FlagName.Decimal));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Decimal).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Overflow_Flag()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Flag(NesIr.FlagName.Overflow));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Negative_Flag()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.Flag(NesIr.FlagName.Negative));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_AllFlags()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.AllFlags());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(0x81);
        testRunner.RunTestMethod();

        testRunner.NesHal.ProcessorStatus.ShouldBe((byte)0x81);
    }

    [Fact]
    public void Can_Pop_To_StackPointer()
    {
        var instruction = new NesIr.PopStackValue(new NesIr.StackPointer());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.PushToStack(0xE5);
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0xE5);
    }
}