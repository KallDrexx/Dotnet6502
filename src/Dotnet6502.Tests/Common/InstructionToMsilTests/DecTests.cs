using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 DEC (Decrement Memory) instruction
///
/// 6502 DEC Behavior:
/// - Decrements memory location by 1
/// - Sets Zero flag if result is 0x00
/// - Sets Negative flag if bit 7 of result is set
/// - Does NOT affect Carry or Overflow flags
/// - Wraparound behavior: 0x00 - 1 = 0xFF
/// </summary>
public class DecTests
{
    [Fact]
    public void DEC_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x10] = 0x05;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x10].ShouldBe((byte)0x04);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPage_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x20] = 0x01;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPage_Wraparound_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x30] = 0x00; // 0x00 - 1 = 0xFF
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x30].ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPage_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC6, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x40] = 0x81; // 0x81 - 1 = 0x80 (negative)
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x40].ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD6, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x55] = 0x10; // 0x50 + 0x05
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x55].ShouldBe((byte)0x0F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD6, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x42; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x01].ShouldBe((byte)0x41);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCE, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x3000] = 0x99;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x98);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue(); // 0x98 has bit 7 set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_Absolute_Positive_To_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCE, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x1234] = 0x01;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x1234].ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xDE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xDE, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x33;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x200F].ShouldBe((byte)0x32);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEC_AbsoluteX_Zero_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xDE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xDE, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x01;
        testRunner.TestHal.MemoryValues[0x5000] = 0x00; // 0x00 - 1 = 0xFF
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x5000].ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue(); // 0xFF has bit 7 set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}