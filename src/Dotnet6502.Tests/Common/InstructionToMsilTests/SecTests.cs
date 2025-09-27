using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear carry flag initially
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        // Carry flag should be set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Carry flag already set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        // Carry flag should remain set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags except carry
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // Only carry flag should be set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;

        testRunner.RunTestMethod();

        // Registers should remain unchanged
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only carry flag should be set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set a mixed pattern of flags
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false; // Will be set
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false; // Should remain false
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;  // Should remain true
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = false; // Should remain false
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;  // Should remain true
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = false; // Should remain false

        testRunner.RunTestMethod();

        // Only carry flag should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should remain unchanged
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear all flags
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = false;

        testRunner.RunTestMethod();

        // Only carry flag should be set
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should remain clear
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear carry flag initially
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;

        // First SEC call
        testRunner.RunTestMethod();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // Second SEC call (should have no effect)
        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.TestHal.Flags[CpuStatusFlags.Carry] = true; // Already set
        testRunner2.RunTestMethod();
        testRunner2.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }
}