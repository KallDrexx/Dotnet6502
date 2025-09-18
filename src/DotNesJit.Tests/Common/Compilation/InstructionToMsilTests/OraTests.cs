using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ORA (Logical OR) instruction
///
/// ORA performs a bitwise OR between the accumulator and a memory value or immediate value.
/// The result is stored in the accumulator.
///
/// Flags affected:
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// - Carry and Overflow flags are NOT affected
/// </summary>
public class OraTests
{
    [Fact]
    public void ORA_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x0F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x10;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x1F);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x00;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x01;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x81);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void ORA_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x05);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x05, 0x10],
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
    public void ORA_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x15);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x15, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x30;
        testRunner.NesHal.XRegister = 0x05;
        testRunner.NesHal.MemoryValues[0x35] = 0x0C;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x3C);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ORA_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x15);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x15, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x01;
        testRunner.NesHal.XRegister = 0x02;
        testRunner.NesHal.MemoryValues[0x01] = 0x7E;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x7F);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ORA_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x0D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x0D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x41;
        testRunner.NesHal.MemoryValues[0x3000] = 0x82;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xC3);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ORA_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x1D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x1D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x0F;
        testRunner.NesHal.MemoryValues[0x200F] = 0x24;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x66);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ORA_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x19);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x19, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x0F;
        testRunner.NesHal.YRegister = 0x01;
        testRunner.NesHal.MemoryValues[0x5000] = 0xE0;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xEF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Pattern_Combine()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0xCC],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x33;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Load_Pattern()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}