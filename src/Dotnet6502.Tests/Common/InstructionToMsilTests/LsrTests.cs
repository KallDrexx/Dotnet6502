using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 LSR (Logical Shift Right) instruction
///
/// LSR shifts all bits right one position. Bit 7 is filled with 0.
/// The original bit 0 is shifted into the Carry flag.
///
/// Flags affected:
/// - Carry: Set to the value of bit 0 before the shift
/// - Zero: Set if result is 0
/// - Negative: Always cleared (bit 7 becomes 0)
/// </summary>
public class LsrTests
{
    [Fact]
    public void LSR_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x46);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x46, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x10] = 0xFE;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x10].ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void LSR_ZeroPage_Carry_And_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x46);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x46, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x20] = 0x01;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x20].ShouldBe((byte)0x00);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void LSR_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x56);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x56, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x05;
        testRunner.TestHal.MemoryValues[0x35] = 0xAA;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x35].ShouldBe((byte)0x55);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void LSR_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x56);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x56, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.XRegister = 0x02;
        testRunner.TestHal.MemoryValues[0x01] = 0x63;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x01].ShouldBe((byte)0x31);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void LSR_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.MemoryValues[0x3000] = 0x88;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x44);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void LSR_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x5E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x5E, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions)
        {
            TestHal =
            {
                XRegister = 0x0F,
                MemoryValues =
                {
                    [0x200F] = 0xFF
                },
                Flags =
                {
                    [CpuStatusFlags.Carry] = false
                }
            }
        };
        testRunner.RunTestMethod();

        testRunner.TestHal.MemoryValues[0x200F].ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void LSR_Accumulator_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.TestHal.ARegister = 0xFE;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x7F);
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}