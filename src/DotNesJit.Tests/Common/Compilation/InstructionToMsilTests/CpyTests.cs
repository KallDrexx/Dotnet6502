using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CPY (Compare Y Register) instruction
///
/// CPY subtracts a memory value or immediate value from the Y register,
/// but does NOT store the result. It only affects the flags.
///
/// Flags affected:
/// - Carry: Set if Y >= memory value (no borrow needed)
/// - Zero: Set if Y == memory value
/// - Negative: Set if bit 7 of (Y - memory value) is set
/// - Overflow flag is NOT affected
///
/// Note: CPY supports fewer addressing modes than CMP (Immediate, ZeroPage, Absolute only)
/// </summary>
public class CpyTests
{
    [Fact]
    public void CPY_Immediate_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x42;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x42);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void CPY_Immediate_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x50;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x50);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void CPY_Immediate_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x30;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x30);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CPY_Immediate_Zero_Compare()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x80;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x80);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CPY_ZeroPage_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC4, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x7F;
        testRunner.NesHal.MemoryValues[0x10] = 0x7F;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x7F);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void CPY_ZeroPage_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC4, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x90;
        testRunner.NesHal.MemoryValues[0x20] = 0x80;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x90);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void CPY_ZeroPage_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC4, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x10;
        testRunner.NesHal.MemoryValues[0x30] = 0x20;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x10);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CPY_Absolute_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCC, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0xAA;
        testRunner.NesHal.MemoryValues[0x3000] = 0xAA;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0xAA);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void CPY_Absolute_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCC, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0xFF;
        testRunner.NesHal.MemoryValues[0x1234] = 0x01;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CPY_Absolute_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCC, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x00;
        testRunner.NesHal.MemoryValues[0x4FFF] = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void CPY_Signed_Comparison()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0x7F;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0x7F);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CPY_Boundary_Values()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.YRegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.YRegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}