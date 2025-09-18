using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 EOR (Exclusive OR) instruction
///
/// EOR performs a bitwise exclusive OR between the accumulator and a memory value or immediate value.
/// The result is stored in the accumulator.
///
/// Flags affected:
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// - Carry and Overflow flags are NOT affected
/// </summary>
public class EorTests
{
    [Fact]
    public void EOR_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x0F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x33;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x3C);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void EOR_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0xFF;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void EOR_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x01;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x7E);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void EOR_Immediate_Flip_Sign()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x01;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x81);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void EOR_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x45);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x45, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x55;
        testRunner.NesHal.MemoryValues[0x10] = 0xAA;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void EOR_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x55);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x55, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x33;
        testRunner.NesHal.XRegister = 0x05;
        testRunner.NesHal.MemoryValues[0x35] = 0x33;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void EOR_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x55);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x55, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x81;
        testRunner.NesHal.XRegister = 0x02;
        testRunner.NesHal.MemoryValues[0x01] = 0x01;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x80);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void EOR_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0xC3;
        testRunner.NesHal.MemoryValues[0x3000] = 0x3C;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void EOR_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x5D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x5D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x0F;
        testRunner.NesHal.MemoryValues[0x200F] = 0x66;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x24);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void EOR_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x59);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x59, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x0F;
        testRunner.NesHal.YRegister = 0x01;
        testRunner.NesHal.MemoryValues[0x5000] = 0xF0;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void EOR_Pattern_Toggle()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x55],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0xAA;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void EOR_Bit_Test()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}