using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 TSX (Transfer Stack Pointer to X) instruction
///
/// TSX transfers the value in the stack pointer to the X register and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// - Does NOT affect the stack pointer (preserves it)
/// </summary>
public class TsxTests
{
    [Fact]
    public void TSX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.StackPointer = 0x42;
        testRunner.NesHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x42);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0x42); // Stack pointer preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TSX_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.StackPointer = 0x00;
        testRunner.NesHal.XRegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x00);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0x00);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TSX_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.StackPointer = 0x80;
        testRunner.NesHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x80);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0x80);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TSX_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.StackPointer = 0xFF;
        testRunner.NesHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TSX_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.StackPointer = 0x7F;
        testRunner.NesHal.XRegister = 0x00;

        // Set some flags that should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x7F);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0x7F);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void TSX_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x11;
        testRunner.NesHal.StackPointer = 0x42;
        testRunner.NesHal.XRegister = 0x00;
        testRunner.NesHal.YRegister = 0x33;
        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x42);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0x42);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        testRunner.NesHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void TSX_Flag_Changes_Override_Previous()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.StackPointer = 0x7F; // Positive value
        testRunner.NesHal.XRegister = 0x00;

        // Set opposite flags that should be overridden
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        testRunner.NesHal.XRegister.ShouldBe((byte)0x7F);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0x7F);
        // Flags should be updated based on the transferred value
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse(); // Overridden
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse(); // Overridden
    }
}