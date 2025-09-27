using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 STX (Store X Register) instruction
///
/// STX stores the X register value to memory and:
/// - Does NOT affect any processor status flags
/// - Simply copies the X register value to the specified memory location
/// </summary>
public class StxTests
{
    [Fact]
    public void STX_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x42;
        jit.TestHal.MemoryValues[0x10] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x10].ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x42); // X register unchanged

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPage_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.MemoryValues[0x20] = 0xFF; // Initial value
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        jit.TestHal.XRegister.ShouldBe((byte)0x00);

        // No flags should be affected even when storing zero
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPage_Negative_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x80; // Negative value
        jit.TestHal.MemoryValues[0x30] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x30].ShouldBe((byte)0x80);
        jit.TestHal.XRegister.ShouldBe((byte)0x80);

        // No flags should be affected even when storing negative value
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPageY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x96);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x96, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x55;
        jit.TestHal.YRegister = 0x05;
        jit.TestHal.MemoryValues[0x45] = 0x00; // 0x40 + 0x05 = 0x45
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x45].ShouldBe((byte)0x55);
        jit.TestHal.XRegister.ShouldBe((byte)0x55);

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPageY_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x96);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x96, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x77;
        jit.TestHal.YRegister = 0x02;
        jit.TestHal.MemoryValues[0x01] = 0x00; // (0xFF + 0x02) & 0xFF = 0x01
        jit.TestHal.MemoryValues[0x101] = 0x00; // Should NOT be written to
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x01].ShouldBe((byte)0x77);
        jit.TestHal.MemoryValues[0x101].ShouldBe((byte)0x00); // Should remain unchanged
        jit.TestHal.XRegister.ShouldBe((byte)0x77);

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x99;
        jit.TestHal.MemoryValues[0x3000] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x99);
        jit.TestHal.XRegister.ShouldBe((byte)0x99);

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Absolute_HighAddress()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x8E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x8E, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xAB;
        jit.TestHal.MemoryValues[0x1234] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x1234].ShouldBe((byte)0xAB);
        jit.TestHal.XRegister.ShouldBe((byte)0xAB);

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Preserves_All_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF;
        // Set all flags that should be preserved
        jit.TestHal.Flags[CpuStatusFlags.Zero] = true;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = true;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x50].ShouldBe((byte)0xFF);
        jit.TestHal.XRegister.ShouldBe((byte)0xFF);

        // All flags should remain exactly as they were
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void STX_Overwrite_Memory()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x60],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xAA;
        jit.TestHal.MemoryValues[0x60] = 0x55; // Existing value to be overwritten
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x60].ShouldBe((byte)0xAA);
        jit.TestHal.XRegister.ShouldBe((byte)0xAA);

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Does_Not_Affect_A_Or_Y_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x70],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0x22;
        jit.TestHal.YRegister = 0x33;
        jit.TestHal.MemoryValues[0x70] = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x70].ShouldBe((byte)0x22);
        jit.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        jit.TestHal.XRegister.ShouldBe((byte)0x22); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_ZeroPageY_Maximum_Y_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x96);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x96, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xCC;
        jit.TestHal.YRegister = 0xFF;
        jit.TestHal.MemoryValues[0xFF] = 0x00; // 0x00 + 0xFF = 0xFF
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0xFF].ShouldBe((byte)0xCC);
        jit.TestHal.XRegister.ShouldBe((byte)0xCC);

        // No flags should be affected
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void STX_Maximum_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x86);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x86, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF; // Maximum value
        jit.TestHal.MemoryValues[0x80] = 0x00; // Initial value
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x80].ShouldBe((byte)0xFF);
        jit.TestHal.XRegister.ShouldBe((byte)0xFF);

        // No flags should be affected even when storing maximum value
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}