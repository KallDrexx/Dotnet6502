using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear all flags initially
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = false;

        // Push a value with specific flags set (Carry, InterruptDisable, Overflow)
        // 0x01 | 0x04 | 0x40 = 0x45
        testRunner.NesHal.PushToStack(0x45);
        testRunner.RunTestMethod();

        // Check flags are set according to pulled value
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();      // bit 0
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();      // bit 1
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue(); // bit 2
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();   // bit 3
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();   // bit 6
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();  // bit 7
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear all flags initially
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = false;

        // Push value with all standard flags set
        // 0x01 | 0x02 | 0x04 | 0x08 | 0x40 | 0x80 = 0xCF
        testRunner.NesHal.PushToStack(0xCF);
        testRunner.RunTestMethod();


        // All flags should be set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags initially
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        // Push value with all flags clear
        testRunner.NesHal.PushToStack(0x00);
        testRunner.RunTestMethod();


        // All flags should be clear
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Start with opposite pattern
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        // Push value with mixed flags: Zero and Negative set
        // 0x02 | 0x80 = 0x82
        testRunner.NesHal.PushToStack(0x82);
        testRunner.RunTestMethod();


        // Check specific flag pattern
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x33;
        testRunner.NesHal.YRegister = 0x77;

        testRunner.NesHal.PushToStack(0xFF);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42); // Should remain unchanged
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test with just carry flag
        var testRunner1 = new InstructionTestRunner(nesIrInstructions);
        testRunner1.NesHal.PushToStack(0x01); // Just carry flag
        testRunner1.RunTestMethod();
        testRunner1.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner1.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();

        // Test with alternating pattern
        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.NesHal.PushToStack(0xAA); // 10101010 pattern
        testRunner2.RunTestMethod();
        testRunner2.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue(); // bit 1
        testRunner2.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue(); // bit 3
        testRunner2.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue(); // bit 7
    }
}