using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.MemoryValues[0x10] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x10].ShouldBe((byte)0x42);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42); // Accumulator unchanged

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.MemoryValues[0x20] = 0xFF; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);

        // No flags should be affected even when storing zero
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x80; // Negative value
        testRunner.TestHal.MemoryValues[0x30] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x30].ShouldBe((byte)0x80);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x80);

        // No flags should be affected even when storing negative value
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x55;
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x45] = 0x00; // 0x40 + 0x05 = 0x45
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x45].ShouldBe((byte)0x55);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x55);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x77;
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x00; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.TestHal.MemoryValues[0x101] = 0x00; // Should NOT be written to
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x01].ShouldBe((byte)0x77);
        testRunner.TestHal.MemoryValues[0x101].ShouldBe((byte)0x00); // Should remain unchanged
        testRunner.TestHal.ARegister.ShouldBe((byte)0x77);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x99;
        testRunner.TestHal.MemoryValues[0x3000] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x99);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x99);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x11;
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x00; // 0x2000 + 0x0F = 0x200F
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x200F].ShouldBe((byte)0x11);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x11);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x22;
        testRunner.TestHal.YRegister = 0x10;
        testRunner.TestHal.MemoryValues[0x4010] = 0x00; // 0x4000 + 0x10 = 0x4010
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x4010].ShouldBe((byte)0x22);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x22);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;

        // Set all flags that should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x50].ShouldBe((byte)0xFF);
        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);

        // All flags should remain exactly as they were
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xAA;
        testRunner.TestHal.MemoryValues[0x60] = 0x55; // Existing value to be overwritten
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x60].ShouldBe((byte)0xAA);
        testRunner.TestHal.ARegister.ShouldBe((byte)0xAA);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}