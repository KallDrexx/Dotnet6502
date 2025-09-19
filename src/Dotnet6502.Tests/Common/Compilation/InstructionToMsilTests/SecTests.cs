using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 SEC (Set Carry Flag) instruction
///
/// SEC sets the carry flag and:
/// - Only affects the carry flag (sets it to 1)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class SecTests
{
    [Fact]
    public void SEC_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear carry flag initially
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        // Carry flag should be set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void SEC_When_Already_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Carry flag already set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        // Carry flag should remain set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void SEC_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags except carry
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // Only carry flag should be set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void SEC_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x33;
        testRunner.NesHal.YRegister = 0x77;
        testRunner.NesHal.StackPointer = 0xFF;
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;

        testRunner.RunTestMethod();

        // Registers should remain unchanged
        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33);
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xFF);

        // Only carry flag should be set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void SEC_With_Mixed_Initial_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set a mixed pattern of flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false; // Will be set
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false; // Should remain false
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;  // Should remain true
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false; // Should remain false
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;  // Should remain true
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = false; // Should remain false

        testRunner.RunTestMethod();

        // Only carry flag should be affected
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should remain unchanged
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SEC_Preserves_Clear_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear all flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = false;

        testRunner.RunTestMethod();

        // Only carry flag should be set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should remain clear
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SEC_Multiple_Calls()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear carry flag initially
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;

        // First SEC call
        testRunner.RunTestMethod();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // Second SEC call (should have no effect)
        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.NesHal.Flags[CpuStatusFlags.Carry] = true; // Already set
        testRunner2.RunTestMethod();
        testRunner2.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }
}