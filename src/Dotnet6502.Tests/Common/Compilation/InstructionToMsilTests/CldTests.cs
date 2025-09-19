using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set decimal flag initially
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.RunTestMethod();

        // Decimal flag should be cleared
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Decimal flag already clear
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.RunTestMethod();

        // Decimal flag should remain clear
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // Only decimal flag should be cleared
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();

        // All other flags should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x33;
        testRunner.NesHal.YRegister = 0x77;
        testRunner.NesHal.StackPointer = 0xFF;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;

        testRunner.RunTestMethod();

        // Registers should remain unchanged
        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33);
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xFF);

        // Only decimal flag should be cleared
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
    }
}