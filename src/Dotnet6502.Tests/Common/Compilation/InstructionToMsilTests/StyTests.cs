using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 STY (Store Y Register) instruction
///
/// STY stores the Y register value to memory and:
/// - Does NOT affect any processor status flags
/// - Simply copies the Y register value to the specified memory location
/// </summary>
public class StyTests
{
    [Fact]
    public void STY_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x42;
        testRunner.TestHal.MemoryValues[0x10] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x10].ShouldBe((byte)0x42);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x42); // Y register unchanged

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_ZeroPage_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x00;
        testRunner.TestHal.MemoryValues[0x20] = 0xFF; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x00);

        // No flags should be affected even when storing zero
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_ZeroPage_Negative_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x80; // Negative value
        testRunner.TestHal.MemoryValues[0x30] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x30].ShouldBe((byte)0x80);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x80);

        // No flags should be affected even when storing negative value
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x55;
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x45] = 0x00; // 0x40 + 0x05 = 0x45
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x45].ShouldBe((byte)0x55);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x55);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x00; // (0xFF + 0x02) & 0xFF = 0x01
        testRunner.TestHal.MemoryValues[0x101] = 0x00; // Should NOT be written to
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x01].ShouldBe((byte)0x77);
        testRunner.TestHal.MemoryValues[0x101].ShouldBe((byte)0x00); // Should remain unchanged
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8C, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x99;
        testRunner.TestHal.MemoryValues[0x3000] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x99);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x99);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_Absolute_HighAddress()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8C, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xAB;
        testRunner.TestHal.MemoryValues[0x1234] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x1234].ShouldBe((byte)0xAB);
        testRunner.TestHal.YRegister.ShouldBe((byte)0xAB);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_Preserves_All_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xFF;

        // Set all flags that should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x50].ShouldBe((byte)0xFF);
        testRunner.TestHal.YRegister.ShouldBe((byte)0xFF);

        // All flags should remain exactly as they were
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void STY_Overwrite_Memory()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xAA;
        testRunner.TestHal.MemoryValues[0x60] = 0x55; // Existing value to be overwritten
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x60].ShouldBe((byte)0xAA);
        testRunner.TestHal.YRegister.ShouldBe((byte)0xAA);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_Does_Not_Affect_A_Or_X_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x70],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x11;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x22;
        testRunner.TestHal.MemoryValues[0x70] = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x70].ShouldBe((byte)0x22);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.TestHal.YRegister.ShouldBe((byte)0x22); // Should remain unchanged

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_ZeroPageX_Maximum_X_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xCC;
        testRunner.TestHal.XRegister = 0xFF;
        testRunner.TestHal.MemoryValues[0xFF] = 0x00; // 0x00 + 0xFF = 0xFF
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0xFF].ShouldBe((byte)0xCC);
        testRunner.TestHal.YRegister.ShouldBe((byte)0xCC);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_Maximum_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x84);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x84, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xFF; // Maximum value
        testRunner.TestHal.MemoryValues[0x80] = 0x00; // Initial value
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x80].ShouldBe((byte)0xFF);
        testRunner.TestHal.YRegister.ShouldBe((byte)0xFF);

        // No flags should be affected even when storing maximum value
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STY_ZeroPageX_Zero_Index()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x94);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x94, 0x90],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xBB;
        testRunner.TestHal.XRegister = 0x00; // No offset
        testRunner.TestHal.MemoryValues[0x90] = 0x00; // 0x90 + 0x00
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x90].ShouldBe((byte)0xBB);
        testRunner.TestHal.YRegister.ShouldBe((byte)0xBB);

        // No flags should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}