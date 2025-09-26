using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CLC (Clear Carry Flag) instruction
///
/// CLC clears the carry flag and:
/// - Only affects the carry flag (sets it to 0)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class ClcTests
{
    [Fact]
    public void CLC_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x18);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x18],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set carry flag initially
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        // Carry flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
    }

    [Fact]
    public void CLC_When_Already_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x18);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x18],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Carry flag already clear
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        // Carry flag should remain clear
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
    }

    [Fact]
    public void CLC_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x18);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x18],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // Only carry flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();

        // All other flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CLC_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x18);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x18],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;

        testRunner.RunTestMethod();

        // Registers should remain unchanged
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only carry flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
    }

    [Fact]
    public void CLC_With_Mixed_Initial_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x18);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x18],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set a mixed pattern of flags
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;  // Will be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false; // Should remain false
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;  // Should remain true
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = false; // Should remain false
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;  // Should remain true
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = false; // Should remain false

        testRunner.RunTestMethod();

        // Only carry flag should be affected
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();

        // All other flags should remain unchanged
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void CLC_Multiple_Calls()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x18);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x18],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set carry flag initially
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;

        // First CLC call
        testRunner.RunTestMethod();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();

        // Second CLC call (should have no effect)
        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.TestHal.Flags[CpuStatusFlags.Carry] = false; // Already clear
        testRunner2.RunTestMethod();
        testRunner2.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
    }
}