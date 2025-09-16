using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ADC (Add with Carry) instruction
///
/// IMPORTANT: These tests expect CORRECT 6502 overflow flag behavior.
/// Correct 6502 behavior: Overflow = (A^result) & (M^result) & 0x80 != 0
///
/// 6502 Overflow Rules:
/// - Overflow occurs when adding two same-sign numbers produces an opposite-sign result
/// - Positive + Positive = Negative → Overflow
/// - Negative + Negative = Positive → Overflow
/// - Positive + Negative → Never overflow (different signs)
/// - Negative + Positive → Never overflow (different signs)
/// </summary>
public class AdcTests
{
    [Fact]
    public void ADC_Immediate_No_Overflow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x34],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 10;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)63);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_With_Carry_Out()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x02;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x01);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: positive + negative = positive, no signed overflow
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x01;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: positive + negative = positive, no signed overflow
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x50;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xA0);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue(); // 6502: positive + positive = negative, signed overflow!
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ADC_Immediate_Overflow_Negative_To_Positive()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x80;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue(); // 6502: negative + negative = positive, signed overflow!
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_With_Carry_In()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x20;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x31);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_Immediate_No_Overflow_Mixed_Signs()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0x80], // -128 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x7F; // +127 in signed
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF); // -1 in signed
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: positive + negative = negative, no signed overflow
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ADC_Immediate_Overflow_Positive_Result_From_Negatives()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x69);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x69, 0xFF], // -1 in signed
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x82; // -126 in signed
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x81); // -127 in signed
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: negative + negative = negative, no signed overflow
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    // ZeroPage addressing mode tests
    [Fact]
    public void ADC_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x65);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x65, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x05;
        testRunner.NesHal.MemoryValues[0x10] = 0x03;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x08);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_ZeroPage_With_Carry()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x65);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x65, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0xFF;
        testRunner.NesHal.MemoryValues[0x20] = 0x02;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x01);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: negative + positive = positive, no signed overflow
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    // ZeroPageX addressing mode tests
    [Fact]
    public void ADC_ZeroPageX_Basic()    {
        var instructionInfo = InstructionSet.GetInstruction(0x75);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x75, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x10;
        testRunner.NesHal.XRegister = 0x05;
        testRunner.NesHal.MemoryValues[0x35] = 0x15; // 0x30 + 0x05
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x25);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_ZeroPageX_Wraparound()    {
        var instructionInfo = InstructionSet.GetInstruction(0x75);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x75, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x01;
        testRunner.NesHal.XRegister = 0x02;
        testRunner.NesHal.MemoryValues[0x01] = 0x07; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.NesHal.MemoryValues[0x101] = 0x07; // In case implementation doesn't wrap
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x08);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    // Absolute addressing mode tests
    [Fact]
    public void ADC_Absolute_Basic()    {
        var instructionInfo = InstructionSet.GetInstruction(0x6D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.MemoryValues[0x3000] = 0x33;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x75);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_Absolute_Overflow()    {
        var instructionInfo = InstructionSet.GetInstruction(0x6D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6D, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x7F;
        testRunner.NesHal.MemoryValues[0x1234] = 0x01;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x80);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue(); // 6502: positive + positive = negative, signed overflow!
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    // AbsoluteX addressing mode tests
    [Fact]
    public void ADC_AbsoluteX_Basic()    {
        var instructionInfo = InstructionSet.GetInstruction(0x7D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x7D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x11;
        testRunner.NesHal.XRegister = 0x0F;
        testRunner.NesHal.MemoryValues[0x200F] = 0x22;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x33);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    // AbsoluteY addressing mode tests
    [Fact]
    public void ADC_AbsoluteY_Basic()    {
        var instructionInfo = InstructionSet.GetInstruction(0x79);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x79, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x08;
        testRunner.NesHal.YRegister = 0x10;
        testRunner.NesHal.MemoryValues[0x4010] = 0x0F;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x17);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ADC_AbsoluteY_Zero_And_Carry()    {
        var instructionInfo = InstructionSet.GetInstruction(0x79);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x79, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x01;
        testRunner.NesHal.YRegister = 0x01;
        testRunner.NesHal.MemoryValues[0x5000] = 0xFE;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse(); // 6502: positive + negative + carry = positive, no signed overflow
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}
