using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using Shouldly;

namespace DotNesJit.Tests.Common.MsilGeneration;

public class WrapValueToByteInstructionTests
{
    [Fact]
    public void Can_WrapValueToByte_NoOverflow_Zero()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Overflow));

        var testRunner = new InstructionTestRunner([setupVar, instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(false);
    }

    [Fact]
    public void Can_WrapValueToByte_NoOverflow_MaxByte()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(255),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Overflow));

        var testRunner = new InstructionTestRunner([setupVar, instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(false);
    }

    [Fact]
    public void Can_WrapValueToByte_Overflow_256()
    {
        // Create 256 using binary operations since Constant only accepts byte
        var setupBase = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Variable(0));
        var setupVar = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(255),
            new NesIr.Constant(1),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Overflow));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupBase, setupVar, instruction, readResult]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(true);
        testRunner.NesHal.ARegister.ShouldBe((byte)0);
    }

    [Fact]
    public void Can_WrapValueToByte_Overflow_511()
    {
        // Create 511 (0x1FF) using binary operations
        var setupBase = new NesIr.Copy(
            new NesIr.Constant(255),
            new NesIr.Variable(0));
        var setupVar = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(256 & 0xFF), // This will be 0, so we use shift
            new NesIr.Variable(0));
        var shiftVar = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftLeft,
            new NesIr.Constant(1),
            new NesIr.Constant(8),
            new NesIr.Variable(1));
        var combineVar = new NesIr.Binary(
            NesIr.BinaryOperator.Or,
            new NesIr.Variable(0),
            new NesIr.Variable(1),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Carry));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([setupBase, setupVar, shiftVar, combineVar, instruction, readResult]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true);
        testRunner.NesHal.XRegister.ShouldBe((byte)255);
    }

    [Fact]
    public void Can_WrapValueToByte_Constant_ToRegister()
    {
        // Create 300 using binary operations
        var setupVar = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Constant(255),
            new NesIr.Constant(45),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Zero));

        var testRunner = new InstructionTestRunner([setupVar, instruction]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true);
    }

    [Fact]
    public void Can_WrapValueToByte_Register_ToFlag()
    {
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            new NesIr.Flag(NesIr.FlagName.Negative));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.ARegister = 200;
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(false);
        testRunner.NesHal.ARegister.ShouldBe((byte)200);
    }

    [Fact]
    public void Can_WrapValueToByte_Memory_ToFlag()
    {
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Memory(0x2000, null),
            new NesIr.Flag(NesIr.FlagName.InterruptDisable));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.WriteMemory(0x2000, 150);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBe(false);
        testRunner.NesHal.ReadMemory(0x2000).ShouldBe((byte)150);
    }

    [Fact]
    public void Can_WrapValueToByte_Variable_ToVariable()
    {
        // Create 1000 using multiple binary operations
        var setupBase = new NesIr.Copy(
            new NesIr.Constant(200),
            new NesIr.Variable(0));
        var add800 = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(200),
            new NesIr.Variable(0));
        var add600 = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(200),
            new NesIr.Variable(0));
        var add400 = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(200),
            new NesIr.Variable(0));
        var add200 = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(200),
            new NesIr.Variable(0));
        var setupFlag = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Variable(1));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Variable(1));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.YIndex));
        var readFlag = new NesIr.Copy(
            new NesIr.Variable(1),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([setupBase, add800, add600, add400, add200, setupFlag, instruction, readResult, readFlag]);
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)(1000 & 0xFF)); // 232
        testRunner.NesHal.XRegister.ShouldBe((byte)1); // overflow flag should be true
    }

    [Fact]
    public void Can_WrapValueToByte_Flag_ToMemory()
    {
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Flag(NesIr.FlagName.Carry),
            new NesIr.Memory(0x3000, null));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.SetFlag(CpuStatusFlags.Carry, true);
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x3000).ShouldBe((byte)0); // no overflow for value 1
        testRunner.NesHal.GetFlag(CpuStatusFlags.Carry).ShouldBe(true); // original flag unchanged
    }

    [Fact]
    public void Can_WrapValueToByte_AllFlags_ToStackPointer()
    {
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.AllFlags(),
            new NesIr.StackPointer());

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.ProcessorStatus = 0xC3;
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0); // no overflow for 0xC3
        testRunner.NesHal.ProcessorStatus.ShouldBe((byte)0xC3); // original flags unchanged
    }

    [Fact]
    public void Can_WrapValueToByte_StackPointer_ToRegister()
    {
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.StackPointer(),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.StackPointer = 0xF8;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0); // no overflow
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xF8); // original value unchanged
    }

    [Fact]
    public void Can_WrapValueToByte_Memory_WithRegisterOffset()
    {
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Memory(0x4000, NesIr.RegisterName.XIndex),
            new NesIr.Flag(NesIr.FlagName.BFlag));

        var testRunner = new InstructionTestRunner([instruction]);
        testRunner.NesHal.XRegister = 10;
        testRunner.NesHal.WriteMemory(0x400A, 75);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.BFlag).ShouldBe(false);
        testRunner.NesHal.ReadMemory(0x400A).ShouldBe((byte)75);
    }

    [Fact]
    public void Can_WrapValueToByte_ToMemory_WithRegisterOffset()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(0),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Memory(0x5000, NesIr.RegisterName.YIndex));

        var testRunner = new InstructionTestRunner([setupVar, instruction]);
        testRunner.NesHal.YRegister = 5;
        testRunner.RunTestMethod();

        testRunner.NesHal.ReadMemory(0x5005).ShouldBe((byte)0); // no overflow for 0
    }

    [Fact]
    public void Can_WrapValueToByte_EdgeCase_NegativeOne()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(unchecked((byte)-1)), // This should be 255
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Decimal));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupVar, instruction, readResult]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Decimal).ShouldBe(false);
        testRunner.NesHal.ARegister.ShouldBe((byte)255);
    }

    [Fact]
    public void Can_WrapValueToByte_EdgeCase_1024()
    {
        // Create 1024 using shift operation (1 << 10)
        var setupVar = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftLeft,
            new NesIr.Constant(1),
            new NesIr.Constant(10),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Overflow));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupVar, instruction, readResult]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(true);
        testRunner.NesHal.ARegister.ShouldBe((byte)0); // 1024 & 0xFF = 0
    }

    [Fact]
    public void Can_WrapValueToByte_EdgeCase_257()
    {
        // Create 257 using binary operations (256 + 1)
        var setupBase = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftLeft,
            new NesIr.Constant(1),
            new NesIr.Constant(8),
            new NesIr.Variable(0));
        var setupVar = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(0),
            new NesIr.Constant(1),
            new NesIr.Variable(0));
        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Overflow));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([setupBase, setupVar, instruction, readResult]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(true);
        testRunner.NesHal.ARegister.ShouldBe((byte)1); // 257 & 0xFF = 1
    }

    [Fact]
    public void Can_WrapValueToByte_MultipleValues_SameInstruction()
    {
        var setupVar1 = new NesIr.Copy(
            new NesIr.Constant(128),
            new NesIr.Variable(0));
        var instruction1 = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Negative));

        // Create 384 using binary operations (256 + 128)
        var setupBase2 = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftLeft,
            new NesIr.Constant(1),
            new NesIr.Constant(8),
            new NesIr.Variable(1));
        var setupVar2 = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            new NesIr.Variable(1),
            new NesIr.Constant(128),
            new NesIr.Variable(1));
        var instruction2 = new NesIr.WrapValueToByte(
            new NesIr.Variable(1),
            new NesIr.Flag(NesIr.FlagName.Zero));

        var readResult1 = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));
        var readResult2 = new NesIr.Copy(
            new NesIr.Variable(1),
            new NesIr.Register(NesIr.RegisterName.XIndex));

        var testRunner = new InstructionTestRunner([
            setupVar1, instruction1, setupBase2, setupVar2, instruction2, readResult1, readResult2
        ]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Negative).ShouldBe(false); // 128 is valid byte
        testRunner.NesHal.GetFlag(CpuStatusFlags.Zero).ShouldBe(true); // 384 has overflow
        testRunner.NesHal.ARegister.ShouldBe((byte)128);
        testRunner.NesHal.XRegister.ShouldBe((byte)(384 & 0xFF)); // 128
    }

    [Fact]
    public void Can_WrapValueToByte_LargeValue_UpperBitsSet()
    {
        var setupVar = new NesIr.Copy(
            new NesIr.Constant(0x78), // Lower 8 bits
            new NesIr.Variable(0));

        // Create upper bits using shift operation
        var createUpperBits = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftLeft,
            new NesIr.Constant(0x12),
            new NesIr.Constant(8),
            new NesIr.Variable(1));
        var combineValues = new NesIr.Binary(
            NesIr.BinaryOperator.Or,
            new NesIr.Variable(0),
            new NesIr.Variable(1),
            new NesIr.Variable(0));

        var instruction = new NesIr.WrapValueToByte(
            new NesIr.Variable(0),
            new NesIr.Flag(NesIr.FlagName.Overflow));
        var readResult = new NesIr.Copy(
            new NesIr.Variable(0),
            new NesIr.Register(NesIr.RegisterName.Accumulator));

        var testRunner = new InstructionTestRunner([
            setupVar, createUpperBits, combineValues, instruction, readResult
        ]);
        testRunner.RunTestMethod();

        testRunner.NesHal.GetFlag(CpuStatusFlags.Overflow).ShouldBe(true);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x78);
    }
}