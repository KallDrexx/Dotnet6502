using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ROL (Rotate Left through Carry) instruction
///
/// ROL shifts all bits left one position. The Carry flag is shifted into bit 0.
/// The original bit 7 is shifted into the Carry flag.
///
/// Flags affected:
/// - Carry: Set to the value of bit 7 before the rotation
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// </summary>
public class RolTests
{
    // Note: ROL Accumulator mode tests are not included as the InstructionConverter
    // does not yet support the Accumulator addressing mode for shift instructions

    [Fact]
    public void ROL_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x26);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x26, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.MemoryValues[0x10] = 0x55;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x10].ShouldBe((byte)0xAA);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ROL_ZeroPage_Carry_In()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x26);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x26, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.MemoryValues[0x20] = 0x7E;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x20].ShouldBe((byte)0xFD);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ROL_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x36);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x36, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x05;
        testRunner.NesHal.MemoryValues[0x35] = 0x81;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x35].ShouldBe((byte)0x02);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ROL_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x36);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x36, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x02;
        testRunner.NesHal.MemoryValues[0x01] = 0x33;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x01].ShouldBe((byte)0x67);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ROL_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x2E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x2E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.MemoryValues[0x3000] = 0x42;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x3000].ShouldBe((byte)0x84);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ROL_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x3E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x3E, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x0F;
        testRunner.NesHal.MemoryValues[0x200F] = 0x01;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x200F].ShouldBe((byte)0x03);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ROL_AbsoluteX_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x3E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x3E, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x01;
        testRunner.NesHal.MemoryValues[0x5000] = 0x80;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x5000].ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}