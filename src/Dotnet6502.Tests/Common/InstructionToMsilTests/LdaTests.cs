using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 LDA (Load Accumulator) instruction
///
/// LDA loads a value from memory or immediate into the accumulator and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// </summary>
public class LdaTests
{
    [Fact]
    public void LDA_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_Immediate_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_Immediate_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_Immediate_HighValue()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA5, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.MemoryValues[0x10] = 0x33;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x33);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPage_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA5, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.TestHal.MemoryValues[0x20] = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB5, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x35] = 0x55; // 0x30 + 0x05
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x55);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB5, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x77; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.TestHal.MemoryValues[0x101] = 0x88; // Should NOT be accessed
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x77);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAD);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAD, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.MemoryValues[0x3000] = 0x99;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x99);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBD);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBD, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x11;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x11);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB9, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.YRegister = 0x10;
        testRunner.TestHal.MemoryValues[0x4010] = 0x22;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x22);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }


    [Fact]
    public void LDA_IndexedIndirect_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0x05;

        // Set up the address lookup: ($20 + X) = $25 contains the target address $3000
        testRunner.TestHal.MemoryValues[0x25] = 0x00; // Low byte of target address
        testRunner.TestHal.MemoryValues[0x26] = 0x30; // High byte of target address

        // Set the value at the target address
        testRunner.TestHal.MemoryValues[0x3000] = 0x42;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.TestHal.XRegister = 0x03;

        // Set up the address lookup: ($10 + X) = $13 contains the target address $2500
        testRunner.TestHal.MemoryValues[0x13] = 0x00; // Low byte of target address
        testRunner.TestHal.MemoryValues[0x14] = 0x25; // High byte of target address

        // Set the value at the target address to 0 to test zero flag
        testRunner.TestHal.MemoryValues[0x2500] = 0x00;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0x08;

        // Set up the address lookup: ($30 + X) = $38 contains the target address $4000
        testRunner.TestHal.MemoryValues[0x38] = 0x00; // Low byte of target address
        testRunner.TestHal.MemoryValues[0x39] = 0x40; // High byte of target address

        // Set the value at the target address to 0x90 to test negative flag
        testRunner.TestHal.MemoryValues[0x4000] = 0x90;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x90);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndexedIndirect_ZeroPageWrapAround()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA1, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0x02;

        // Test zero page wraparound: ($FF + 0x02) wraps to $01,$02 (not $101,$102)
        testRunner.TestHal.MemoryValues[0x01] = 0x50; // Low byte of target address
        testRunner.TestHal.MemoryValues[0x02] = 0x15; // High byte of target address

        // Set the value at the target address
        testRunner.TestHal.MemoryValues[0x1550] = 0x77;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x77);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.YRegister = 0x10;

        // Set up the base address: $30 contains the base address $2000
        testRunner.TestHal.MemoryValues[0x30] = 0x00; // Low byte of base address
        testRunner.TestHal.MemoryValues[0x31] = 0x20; // High byte of base address

        // Set the value at the final address ($2000 + Y) = $2010
        testRunner.TestHal.MemoryValues[0x2010] = 0x84;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x84);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.TestHal.YRegister = 0x05;

        // Set up the base address: $40 contains the base address $3000
        testRunner.TestHal.MemoryValues[0x40] = 0x00; // Low byte of base address
        testRunner.TestHal.MemoryValues[0x41] = 0x30; // High byte of base address

        // Set the value at the final address ($3000 + Y) = $3005 to 0
        testRunner.TestHal.MemoryValues[0x3005] = 0x00;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.YRegister = 0x20;

        // Set up the base address: $50 contains the base address $4000
        testRunner.TestHal.MemoryValues[0x50] = 0x00; // Low byte of base address
        testRunner.TestHal.MemoryValues[0x51] = 0x40; // High byte of base address

        // Set the value at the final address ($4000 + Y) = $4020 to 0xFF
        testRunner.TestHal.MemoryValues[0x4020] = 0xFF;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_IndirectIndexed_PageBoundary()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB1);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB1, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.YRegister = 0xFF;

        // Set up the base address: $60 contains the base address $20FF
        testRunner.TestHal.MemoryValues[0x60] = 0xFF; // Low byte of base address
        testRunner.TestHal.MemoryValues[0x61] = 0x20; // High byte of base address

        // Final address ($20FF + Y) = $21FE crosses page boundary
        testRunner.TestHal.MemoryValues[0x21FE] = 0x33;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x33);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDA_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA9, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;

        // Set some flags that should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }
}