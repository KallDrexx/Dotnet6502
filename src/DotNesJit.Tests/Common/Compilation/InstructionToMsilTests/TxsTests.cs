using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 TXS (Transfer X to Stack Pointer) instruction
///
/// TXS transfers the value in the X register to the stack pointer and:
/// - Does NOT affect any flags (unique among transfer instructions)
/// - Does NOT affect the X register (preserves it)
/// - Does NOT affect other registers
/// </summary>
public class TxsTests
{
    [Fact]
    public void TXS_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x42;
        testRunner.NesHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x42); // X register preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TXS_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x00;
        testRunner.NesHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0x00);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x00);
        // TXS does NOT set zero flag even when transferring zero
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void TXS_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x80;
        testRunner.NesHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0x80);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x80);
        // TXS does NOT set negative flag even when transferring negative value
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void TXS_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0xFF;
        testRunner.NesHal.StackPointer = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0xFF);
        testRunner.NesHal.XRegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void TXS_Preserves_All_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x80; // Negative value
        testRunner.NesHal.StackPointer = 0xFF;

        // Set all flags to test they are preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0x80);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x80);

        // All flags should be preserved (TXS is unique in not affecting flags)
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void TXS_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x11;
        testRunner.NesHal.XRegister = 0x42;
        testRunner.NesHal.YRegister = 0x33;
        testRunner.NesHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x42);
        testRunner.NesHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        testRunner.NesHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged
    }

    [Fact]
    public void TXS_Preserves_Clear_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.XRegister = 0x00; // Zero value
        testRunner.NesHal.StackPointer = 0xFF;

        // Clear all flags to test they remain clear
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = false;

        testRunner.RunTestMethod();

        testRunner.NesHal.StackPointer.ShouldBe((byte)0x00);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x00);

        // All flags should remain clear
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}