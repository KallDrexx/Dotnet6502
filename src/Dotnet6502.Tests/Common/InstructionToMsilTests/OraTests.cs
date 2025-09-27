using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ORA (Logical OR) instruction
///
/// ORA performs a bitwise OR between the accumulator and a memory value or immediate value.
/// The result is stored in the accumulator.
///
/// Flags affected:
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// - Carry and Overflow flags are NOT affected
/// </summary>
public class OraTests
{
    [Fact]
    public void ORA_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x0F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x10;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x1F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x01;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x81);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void ORA_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x05);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x05, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x55;
        testRunner.TestHal.MemoryValues[0x10] = 0xAA;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ORA_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x15);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x15, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x30;
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x35] = 0x0C;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x3C);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ORA_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x15);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x15, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x01;
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x7E;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ORA_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x0D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x0D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x41;
        testRunner.TestHal.MemoryValues[0x3000] = 0x82;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xC3);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ORA_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x1D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x1D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x0F;
        testRunner.TestHal.MemoryValues[0x200F] = 0x24;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x66);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ORA_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x19);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x19, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x0F;
        testRunner.TestHal.YRegister = 0x01;
        testRunner.TestHal.MemoryValues[0x5000] = 0xE0;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xEF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Pattern_Combine()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0xCC],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x33;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ORA_Load_Pattern()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}