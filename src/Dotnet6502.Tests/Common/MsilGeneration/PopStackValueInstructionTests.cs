using Dotnet6502.Common;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class PopStackValueInstructionTests
{
    [Fact]
    public void Can_Pop_To_Accumulator()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(123);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)123);
    }

    [Fact]
    public void Can_Pop_To_XIndex()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(55);
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)55);
    }

    [Fact]
    public void Can_Pop_To_YIndex()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Register(Ir6502.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(88);
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)88);
    }

    [Fact]
    public void Can_Pop_To_Memory()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Memory(0x3000, null, false));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(199);
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x3000).ShouldBe((byte)199);
    }

    [Fact]
    public void Can_Pop_To_Memory_With_Register_Offset()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Memory(0x5000, Ir6502.RegisterName.XIndex, false));

        var testRunner = new InstructionTestRunner([instruction])
        {
            TestHal =
            {
                XRegister = 10
            }
        };
        testRunner.TestHal.PushToStack(222);
        testRunner.RunTestMethod();

        testRunner.TestHal.ReadMemory(0x500A).ShouldBe((byte)222);
    }

    [Fact]
    public void Can_Pop_To_Variable()
    {
        var popInstruction = new Ir6502.PopStackValue(new Ir6502.Variable(0));
        var copyInstruction = new Ir6502.Copy(
            new Ir6502.Variable(0),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([popInstruction, copyInstruction]);
        testRunner.TestHal.PushToStack(177);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)177);
    }

    [Fact]
    public void Can_Pop_To_Carry_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Carry));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Zero_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Zero));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(0);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(false);
    }

    [Fact]
    public void Can_Pop_To_InterruptDisable_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_BFlag_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.BFlag));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.BFlag).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Decimal_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Decimal));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Overflow_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_Negative_Flag()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.Flag(Ir6502.FlagName.Negative));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(1);
        testRunner.RunTestMethod();

        testRunner.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(true);
    }

    [Fact]
    public void Can_Pop_To_AllFlags()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.AllFlags());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(0x81);
        testRunner.RunTestMethod();

        testRunner.TestHal.ProcessorStatus.ShouldBe((byte)0x81);
    }

    [Fact]
    public void Can_Pop_To_StackPointer()
    {
        var instruction = new Ir6502.PopStackValue(new Ir6502.StackPointer());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.TestHal.PushToStack(0xE5);
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0xE5);
    }
}