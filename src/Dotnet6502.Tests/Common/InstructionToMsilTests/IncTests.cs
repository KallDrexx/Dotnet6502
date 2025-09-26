using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 INC (Increment Memory) instruction
///
/// 6502 INC Behavior:
/// - Increments memory location by 1
/// - Sets Zero flag if result is 0x00
/// - Sets Negative flag if bit 7 of result is set
/// - Does NOT affect Carry or Overflow flags
/// - Wraparound behavior: 0xFF + 1 = 0x00
/// </summary>
public class IncTests
{
    [Fact]
    public void INC_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE6, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x10] = 0x05;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x10].ShouldBe((byte)0x06);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_ZeroPage_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE6, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x20] = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_ZeroPage_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE6, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x30] = 0x7F; // 127, increment to 128 (0x80, negative)
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x30].ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF6, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x45] = 0x10; // 0x40 + 0x05
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x45].ShouldBe((byte)0x11);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF6, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x42; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x01].ShouldBe((byte)0x43);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xEE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xEE, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x3000] = 0x99;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x9A);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue(); // 0x9A has bit 7 set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_Absolute_Wraparound_To_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xEE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xEE, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x1234] = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x1234].ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xFE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xFE, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x33;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x200F].ShouldBe((byte)0x34);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INC_AbsoluteX_Negative_To_Positive()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xFE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xFE, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x01;
        testRunner.TestHal.MemoryValues[0x5000] = 0xFE; // Negative value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x5000].ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue(); // 0xFF still has bit 7 set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}