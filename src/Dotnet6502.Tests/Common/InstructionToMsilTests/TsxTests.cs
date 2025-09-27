using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.StackPointer = 0x42;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x42);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0x42); // Stack pointer preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.StackPointer = 0x00;
        testRunner.TestHal.XRegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x00);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.StackPointer = 0x80;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x80);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.StackPointer = 0x7F;
        testRunner.TestHal.XRegister = 0x00;

        // Set some flags that should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x11;
        testRunner.TestHal.StackPointer = 0x42;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.TestHal.YRegister = 0x33;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x42);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0x42);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        testRunner.TestHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.StackPointer = 0x7F; // Positive value
        testRunner.TestHal.XRegister = 0x00;

        // Set opposite flags that should be overridden
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0x7F);
        // Flags should be updated based on the transferred value
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse(); // Overridden
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse(); // Overridden
    }
}