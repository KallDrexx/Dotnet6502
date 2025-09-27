using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 PLP (Pull Processor Status) instruction
///
/// PLP pulls a value from the stack into the processor status register (flags) and:
/// - Affects ALL flags based on the pulled value
/// - Increments the stack pointer
/// - Does NOT affect registers
/// - Ignores the Break flag in the pulled value (real 6502 behavior)
/// </summary>
public class PlpTests
{
    [Fact]
    public void PLP_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear all flags initially
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.TestHal.Flags[CpuStatusFlags.Zero] = false;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = false;

        // Push a value with specific flags set (Carry, InterruptDisable, Overflow)
        // 0x01 | 0x04 | 0x40 = 0x45
        jit.TestHal.PushToStack(0x45);
        jit.RunMethod(0x1234);

        // Check flags are set according to pulled value
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();      // bit 0
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();      // bit 1
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue(); // bit 2
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();   // bit 3
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();   // bit 6
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();  // bit 7
    }

    [Fact]
    public void PLP_All_Flags_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear all flags initially
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.TestHal.Flags[CpuStatusFlags.Zero] = false;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = false;

        // Push value with all standard flags set
        // 0x01 | 0x02 | 0x04 | 0x08 | 0x40 | 0x80 = 0xCF
        jit.TestHal.PushToStack(0xCF);
        jit.RunMethod(0x1234);


        // All flags should be set
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void PLP_All_Flags_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags initially
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.TestHal.Flags[CpuStatusFlags.Zero] = true;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = true;

        // Push value with all flags clear
        jit.TestHal.PushToStack(0x00);
        jit.RunMethod(0x1234);


        // All flags should be clear
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void PLP_Mixed_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Start with opposite pattern
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.TestHal.Flags[CpuStatusFlags.Zero] = true;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = true;

        // Push value with mixed flags: Zero and Negative set
        // 0x02 | 0x80 = 0x82
        jit.TestHal.PushToStack(0x82);
        jit.RunMethod(0x1234);


        // Check specific flag pattern
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void PLP_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;

        jit.TestHal.PushToStack(0xFF);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42); // Should remain unchanged
        jit.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
    }

    [Fact]
    public void PLP_Edge_Cases()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test with just carry flag
        var jit1 = new TestJitCompiler();
            jit1.AddMethod(0x1234, nesIrInstructions);
        jit1.TestHal.PushToStack(0x01); // Just carry flag
        jit1.RunMethod(0x1234);
        jit1.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit1.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();

        // Test with alternating pattern
        var jit2 = new TestJitCompiler();
            jit2.AddMethod(0x1234, nesIrInstructions);
        jit2.TestHal.PushToStack(0xAA); // 10101010 pattern
        jit2.RunMethod(0x1234);
        jit2.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue(); // bit 1
        jit2.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue(); // bit 3
        jit2.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue(); // bit 7
    }
}