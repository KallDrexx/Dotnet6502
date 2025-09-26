using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ASL (Arithmetic Shift Left) instruction
///
/// ASL shifts all bits left one position. Bit 0 is filled with 0.
/// The original bit 7 is shifted into the Carry flag.
///
/// Flags affected:
/// - Carry: Set to the value of bit 7 before the shift
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// </summary>
public class AslTests
{
    // Note: ASL Accumulator mode tests are not included as the InstructionConverter
    // does not yet support the Accumulator addressing mode for shift instructions

    [Fact]
    public void ASL_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x06);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x06, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x10] = 0x20;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x10].ShouldBe((byte)0x40);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ASL_ZeroPage_Carry_And_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x06);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x06, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x20] = 0xC0;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x20].ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ASL_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x16);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x16, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x35] = 0x15;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x35].ShouldBe((byte)0x2A);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ASL_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x16);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x16, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x33;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x01].ShouldBe((byte)0x66);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ASL_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x0E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x0E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x3000] = 0x42;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x84);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ASL_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x1E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x1E, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x7F;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x200F].ShouldBe((byte)0xFE);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ASL_AbsoluteX_Carry_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x1E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x1E, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x01;
        testRunner.TestHal.MemoryValues[0x5000] = 0x80;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x5000].ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}