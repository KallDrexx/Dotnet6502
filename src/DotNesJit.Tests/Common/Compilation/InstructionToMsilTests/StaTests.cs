using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 STA (Store Accumulator) instruction
///
/// STA stores the accumulator value to memory and:
/// - Does NOT affect any processor status flags
/// - Simply copies the accumulator value to the specified memory location
/// </summary>
public class StaTests
{
    [Fact]
    public void STA_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.MemoryValues[0x10] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x10].ShouldBe((byte)0x42);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x42); // Accumulator unchanged

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STA_ZeroPage_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x00;
        testRunner.NesHal.MemoryValues[0x20] = 0xFF; // Initial value
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);

        // No flags should be affected even when storing zero
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STA_ZeroPage_Negative_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x80; // Negative value
        testRunner.NesHal.MemoryValues[0x30] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x30].ShouldBe((byte)0x80);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x80);

        // No flags should be affected even when storing negative value
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STA_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x95);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x95, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x55;
        testRunner.NesHal.XRegister = 0x05;
        testRunner.NesHal.MemoryValues[0x45] = 0x00; // 0x40 + 0x05 = 0x45
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x45].ShouldBe((byte)0x55);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x55);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STA_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x95);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x95, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x77;
        testRunner.NesHal.XRegister = 0x02;
        testRunner.NesHal.MemoryValues[0x01] = 0x00; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.NesHal.MemoryValues[0x101] = 0x00; // Should NOT be written to
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x01].ShouldBe((byte)0x77);
        testRunner.NesHal.MemoryValues[0x101].ShouldBe((byte)0x00); // Should remain unchanged
        testRunner.NesHal.ARegister.ShouldBe((byte)0x77);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STA_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x99;
        testRunner.NesHal.MemoryValues[0x3000] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x3000].ShouldBe((byte)0x99);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x99);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STA_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x11;
        testRunner.NesHal.XRegister = 0x0F;
        testRunner.NesHal.MemoryValues[0x200F] = 0x00; // 0x2000 + 0x0F = 0x200F
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x200F].ShouldBe((byte)0x11);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x11);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STA_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x99);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x99, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x22;
        testRunner.NesHal.YRegister = 0x10;
        testRunner.NesHal.MemoryValues[0x4010] = 0x00; // 0x4000 + 0x10 = 0x4010
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x4010].ShouldBe((byte)0x22);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x22);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }


    [Fact]
    public void STA_Preserves_All_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0xFF;

        // Set all flags that should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x50].ShouldBe((byte)0xFF);
        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);

        // All flags should remain exactly as they were
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void STA_Overwrite_Memory()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x85);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x85, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0xAA;
        testRunner.NesHal.MemoryValues[0x60] = 0x55; // Existing value to be overwritten
        testRunner.RunTestMethod();

        testRunner.NesHal.MemoryValues[0x60].ShouldBe((byte)0xAA);
        testRunner.NesHal.ARegister.ShouldBe((byte)0xAA);

        // No flags should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}