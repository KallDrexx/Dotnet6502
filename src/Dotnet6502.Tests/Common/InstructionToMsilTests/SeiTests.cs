using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 SEI (Set Interrupt Disable Flag) instruction
///
/// SEI sets the interrupt disable flag and:
/// - Only affects the interrupt disable flag (sets it to 1)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class SeiTests
{
    [Fact]
    public void SEI_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x78);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x78],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear interrupt disable flag initially
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.RunTestMethod();

        // Interrupt disable flag should be set
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void SEI_When_Already_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x78);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x78],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Interrupt disable flag already set
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.RunTestMethod();

        // Interrupt disable flag should remain set
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void SEI_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x78);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x78],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags except interrupt disable
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // Only interrupt disable flag should be set
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();

        // All other flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void SEI_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x78);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x78],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;

        testRunner.RunTestMethod();

        // Registers should remain unchanged
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only interrupt disable flag should be set
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }
}