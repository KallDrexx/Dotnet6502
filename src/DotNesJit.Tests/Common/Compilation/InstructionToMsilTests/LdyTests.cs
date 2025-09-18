using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 LDY (Load Y Register) instruction
///
/// LDY loads a value from memory or immediate into the Y register and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// </summary>
public class LdyTests
{
    [Fact]
    public void LDY_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x42);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_Immediate_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_Immediate_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x80);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_Immediate_HighValue()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA4, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.MemoryValues[0x10] = 0x33;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x33);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPage_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA4, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0xFF;
        testRunner.NesHal.MemoryValues[0x20] = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB4, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.XRegister = 0x05;
        testRunner.NesHal.MemoryValues[0x35] = 0x55; // 0x30 + 0x05
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x55);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB4, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.XRegister = 0x02;
        testRunner.NesHal.MemoryValues[0x01] = 0x77; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.NesHal.MemoryValues[0x101] = 0x88; // Should NOT be accessed
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x77);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAC, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.MemoryValues[0x3000] = 0x99;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x99);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBC, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.XRegister = 0x10;
        testRunner.NesHal.MemoryValues[0x4010] = 0x22;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x22);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_AbsoluteX_PageCrossing()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBC, 0xFF, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.XRegister = 0x02;
        testRunner.NesHal.MemoryValues[0x2101] = 0x88; // 0x20FF + 0x02 = 0x2101
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x88);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;

        // Set some flags that should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x7F);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void LDY_Does_Not_Affect_A_Or_X_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA0, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x11;
        testRunner.NesHal.XRegister = 0x22;
        testRunner.NesHal.YRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x42);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        testRunner.NesHal.XRegister.ShouldBe((byte)0x22); // Should remain unchanged
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_ZeroPageX_Negative_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB4, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.XRegister = 0x08;
        testRunner.NesHal.MemoryValues[0x48] = 0xC0; // 0x40 + 0x08, negative value
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0xC0);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_Absolute_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAC, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0xFF;
        testRunner.NesHal.MemoryValues[0x1234] = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDY_AbsoluteX_Zero_Index()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBC, 0x50, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.XRegister = 0x00; // No offset
        testRunner.NesHal.MemoryValues[0x3050] = 0x66; // 0x3050 + 0x00
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x66);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}