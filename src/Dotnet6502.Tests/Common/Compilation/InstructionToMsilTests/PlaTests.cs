using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

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
        testRunner.NesHal.ARegister = 0x00;
        testRunner.NesHal.PushToStack(0x42);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
        testRunner.NesHal.ARegister = 0xFF;
        testRunner.NesHal.PushToStack(0x00);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
        testRunner.NesHal.ARegister = 0x00;
        testRunner.NesHal.PushToStack(0x80);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x80);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
        testRunner.NesHal.ARegister = 0x00;
        testRunner.NesHal.PushToStack(0xFF);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
        testRunner.NesHal.ARegister = 0x00;
        testRunner.NesHal.PushToStack(0x7F);

        // Set some flags that should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x7F);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
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
        testRunner.NesHal.ARegister = 0x00;
        testRunner.NesHal.XRegister = 0x33;
        testRunner.NesHal.YRegister = 0x77;
        testRunner.NesHal.PushToStack(0x42);
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
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
        testRunner1.NesHal.ARegister = 0x00;
        testRunner1.NesHal.PushToStack(0x55);
        testRunner1.RunTestMethod();
        testRunner1.NesHal.ARegister.ShouldBe((byte)0x55);
        testRunner1.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner1.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.NesHal.ARegister = 0xFF;
        testRunner2.NesHal.PushToStack(0xAA);
        testRunner2.RunTestMethod();
        testRunner2.NesHal.ARegister.ShouldBe((byte)0xAA);
        testRunner2.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner2.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }
}