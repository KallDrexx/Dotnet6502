using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CLD (Clear Decimal Flag) instruction
///
/// CLD clears the decimal flag and:
/// - Only affects the decimal flag (sets it to 0)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class CldTests
{
    [Fact]
    public void CLD_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set decimal flag initially
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.RunTestMethod();

        // Decimal flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
    }

    [Fact]
    public void CLD_When_Already_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Decimal flag already clear
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.RunTestMethod();

        // Decimal flag should remain clear
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
    }

    [Fact]
    public void CLD_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

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

        // Only decimal flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();

        // All other flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CLD_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;

        testRunner.RunTestMethod();

        // Registers should remain unchanged
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only decimal flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
    }
}