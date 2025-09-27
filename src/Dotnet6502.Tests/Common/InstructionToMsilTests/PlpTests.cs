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
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear all flags initially
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = false;

        // Push a value with specific flags set (Carry, InterruptDisable, Overflow)
        // 0x01 | 0x04 | 0x40 = 0x45
        testRunner.TestHal.PushToStack(0x45);
        testRunner.RunTestMethod();

        // Check flags are set according to pulled value
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();      // bit 0
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();      // bit 1
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue(); // bit 2
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();   // bit 3
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();   // bit 6
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();  // bit 7
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
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear all flags initially
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = false;

        // Push value with all standard flags set
        // 0x01 | 0x02 | 0x04 | 0x08 | 0x40 | 0x80 = 0xCF
        testRunner.TestHal.PushToStack(0xCF);
        testRunner.RunTestMethod();


        // All flags should be set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags initially
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        // Push value with all flags clear
        testRunner.TestHal.PushToStack(0x00);
        testRunner.RunTestMethod();


        // All flags should be clear
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Start with opposite pattern
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        // Push value with mixed flags: Zero and Negative set
        // 0x02 | 0x80 = 0x82
        testRunner.TestHal.PushToStack(0x82);
        testRunner.RunTestMethod();


        // Check specific flag pattern
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;

        testRunner.TestHal.PushToStack(0xFF);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42); // Should remain unchanged
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
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
        var testRunner1 = new InstructionTestRunner(nesIrInstructions);
        testRunner1.TestHal.PushToStack(0x01); // Just carry flag
        testRunner1.RunTestMethod();
        testRunner1.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner1.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();

        // Test with alternating pattern
        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.TestHal.PushToStack(0xAA); // 10101010 pattern
        testRunner2.RunTestMethod();
        testRunner2.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue(); // bit 1
        testRunner2.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue(); // bit 3
        testRunner2.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue(); // bit 7
    }
}