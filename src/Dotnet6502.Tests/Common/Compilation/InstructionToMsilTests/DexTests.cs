using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 DEX (Decrement X Register) instruction
///
/// 6502 DEX Behavior:
/// - Decrements X register by 1
/// - Sets Zero flag if result is 0x00
/// - Sets Negative flag if bit 7 of result is set
/// - Does NOT affect Carry or Overflow flags
/// - Wraparound behavior: 0x00 - 1 = 0xFF
/// </summary>
public class DexTests
{
    [Fact]
    public void DEX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x05;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x04);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEX_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x01; // 0x01 - 1 = 0x00
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEX_Wraparound_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x00; // 0x00 - 1 = 0xFF (wraparound)
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEX_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x81; // 0x81 - 1 = 0x80 (still negative)
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x80);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEX_Positive_Non_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x42;
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x41);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEX_From_Maximum_Positive()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x7F; // 127, decrement to 126 (0x7E, still positive)
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x7E);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEX_From_Negative_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0xFE; // -2, decrement to -3 (0xFD)
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0xFD);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void DEX_Boundary_Value_0x80()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x80; // -128, decrement to 127 (0x7F, becomes positive)
        testRunner.RunTestMethod();

        testRunner.TestHal.XRegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}