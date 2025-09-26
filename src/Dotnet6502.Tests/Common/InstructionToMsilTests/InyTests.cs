using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 INY (Increment Y Register) instruction
///
/// 6502 INY Behavior:
/// - Increments Y register by 1
/// - Sets Zero flag if result is 0x00
/// - Sets Negative flag if bit 7 of result is set
/// - Does NOT affect Carry or Overflow flags
/// - Wraparound behavior: 0xFF + 1 = 0x00
/// </summary>
public class InyTests
{
    [Fact]
    public void INY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x05;
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0x06);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INY_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xFF; // 0xFF + 1 = 0x00 (wraparound)
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INY_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x7F; // 127, increment to 128 (0x80, negative)
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INY_Positive_Non_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x42;
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0x43);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INY_From_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0x01);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INY_From_Negative_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x80; // -128, increment to -127 (0x81)
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0x81);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INY_Boundary_Value_0x7E()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0x7E; // 126, increment to 127 (0x7F, still positive)
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void INY_Negative_To_More_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.YRegister = 0xFE; // -2, increment to -1 (0xFF)
        testRunner.RunTestMethod();

        testRunner.TestHal.YRegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}