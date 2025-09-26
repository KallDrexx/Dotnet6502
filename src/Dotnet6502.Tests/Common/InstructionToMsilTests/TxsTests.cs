using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

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
        testRunner.TestHal.XRegister = 0x42;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x42); // X register preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
        testRunner.TestHal.XRegister = 0x00;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0x00);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x00);
        // TXS does NOT set zero flag even when transferring zero
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
        testRunner.TestHal.XRegister = 0x80;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0x80);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x80);
        // TXS does NOT set negative flag even when transferring negative value
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
        testRunner.TestHal.XRegister = 0xFF;
        testRunner.TestHal.StackPointer = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);
        testRunner.TestHal.XRegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
        testRunner.TestHal.XRegister = 0x80; // Negative value
        testRunner.TestHal.StackPointer = 0xFF;

        // Set all flags to test they are preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0x80);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x80);

        // All flags should be preserved (TXS is unique in not affecting flags)
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
        testRunner.TestHal.ARegister = 0x11;
        testRunner.TestHal.XRegister = 0x42;
        testRunner.TestHal.YRegister = 0x33;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x42);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        testRunner.TestHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged
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
        testRunner.TestHal.XRegister = 0x00; // Zero value
        testRunner.TestHal.StackPointer = 0xFF;

        // Clear all flags to test they remain clear
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = false;

        testRunner.RunTestMethod();

        testRunner.TestHal.StackPointer.ShouldBe((byte)0x00);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x00);

        // All flags should remain clear
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}