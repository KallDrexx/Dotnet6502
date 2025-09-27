using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 TAX (Transfer Accumulator to X) instruction
///
/// TAX transfers the value in the accumulator to the X register and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// </summary>
public class TaxTests
{
    [Fact]
    public void TAX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x42);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TAX_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.XRegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x00);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TAX_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x80;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x80);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TAX_HighValue()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void TAX_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x7F;
        testRunner.TestHal.XRegister = 0x00;

        // Set some flags that should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x7F);
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
    public void TAX_Does_Not_Affect_Y_Register()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x00;
        testRunner.TestHal.YRegister = 0x33;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x42);
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}