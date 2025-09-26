using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 PLA (Pull Accumulator) instruction
///
/// PLA pulls a value from the stack into the accumulator and:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// - Increments the stack pointer
/// </summary>
public class PlaTests
{
    [Fact]
    public void PLA_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.PushToStack(0x42);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void PLA_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.TestHal.PushToStack(0x00);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void PLA_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.PushToStack(0x80);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void PLA_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.PushToStack(0xFF);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void PLA_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.PushToStack(0x7F);

        // Set some flags that should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void PLA_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.PushToStack(0x42);
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
    }

    [Fact]
    public void PLA_Multiple_Values()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test pulling multiple different values
        var testRunner1 = new InstructionTestRunner(nesIrInstructions);
        testRunner1.TestHal.ARegister = 0x00;
        testRunner1.TestHal.PushToStack(0x55);
        testRunner1.RunTestMethod();
        testRunner1.TestHal.ARegister.ShouldBe((byte)0x55);
        testRunner1.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner1.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.TestHal.ARegister = 0xFF;
        testRunner2.TestHal.PushToStack(0xAA);
        testRunner2.RunTestMethod();
        testRunner2.TestHal.ARegister.ShouldBe((byte)0xAA);
        testRunner2.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner2.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }
}